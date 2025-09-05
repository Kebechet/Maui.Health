using Foundation;
using HealthKit;
using Maui.Health.Enums;
using Maui.Health.Enums.Errors;
using Maui.Health.Models;
using Maui.Health.Models.Metrics;
using Maui.Health.Extensions;
using Maui.Health.Platforms.iOS.Extensions;

namespace Maui.Health.Services;

public partial class HealthService
{
    public partial bool IsSupported => HKHealthStore.IsHealthDataAvailable;

    public async partial Task<TDto[]> GetHealthDataAsync<TDto>(DateTime from, DateTime to, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        if (!IsSupported)
        {
            return [];
        }

        try
        {
            // Request permission for the specific metric
            var permission = MetricDtoExtensions.GetRequiredPermission<TDto>();
            var permissionResult = await RequestPermissions([permission], cancellationToken: cancellationToken);
            if (!permissionResult.IsSuccess)
            {
                return [];
            }

            var healthDataType = MetricDtoExtensions.GetHealthDataType<TDto>();
            var quantityType = HKQuantityType.Create(healthDataType.ToHKQuantityTypeIdentifier())!;

            var predicate = HKQuery.GetPredicateForSamples(
                (NSDate)from,
                (NSDate)to,
                HKQueryOptions.StrictStartDate
            );

            var tcs = new TaskCompletionSource<TDto[]>();

            // Use HKSampleQuery to get individual records
            var query = new HKSampleQuery(
                quantityType,
                predicate,
                0, // No limit
                new[] { new NSSortDescriptor(HKSample.SortIdentifierStartDate, false) },
                (HKSampleQuery sampleQuery, HKSample[] results, NSError error) =>
                {
                    if (error != null)
                    {
                        tcs.TrySetResult([]);
                        return;
                    }

                    var dtos = new List<TDto>();
                    foreach (var sample in results.OfType<HKQuantitySample>())
                    {
                        var dto = ConvertToDto<TDto>(sample, healthDataType);
                        if (dto is not null)
                        {
                            dtos.Add(dto);
                        }
                    }

                    tcs.TrySetResult(dtos.ToArray());
                }
            );

            using var store = new HKHealthStore();
            using var ct = cancellationToken.Register(() =>
            {
                tcs.TrySetCanceled();
                store.StopQuery(query);
            });

            store.ExecuteQuery(query);
            return await tcs.Task;
        }
        catch (Exception)
        {
            return [];
        }
    }

    private TDto? ConvertToDto<TDto>(HKQuantitySample sample, HealthDataType healthDataType) where TDto : HealthMetricBase
    {
        return typeof(TDto).Name switch
        {
            nameof(StepsDto) => ConvertStepsSample(sample) as TDto,
            nameof(WeightDto) => ConvertWeightSample(sample) as TDto,
            nameof(HeightDto) => ConvertHeightSample(sample) as TDto,
            _ => null
        };
    }

    private StepsDto ConvertStepsSample(HKQuantitySample sample)
    {
        var value = sample.Quantity.GetDoubleValue(HKUnit.Count);
        var startTime = new DateTimeOffset(sample.StartDate.ToDateTime());
        var endTime = new DateTimeOffset(sample.EndDate.ToDateTime());
        
        return new StepsDto
        {
            Id = sample.Uuid.ToString(),
            DataOrigin = sample.SourceRevision?.Source?.Name ?? "Unknown",
            Timestamp = startTime, // Use start time as the representative timestamp
            Count = (long)value,
            StartTime = startTime,
            EndTime = endTime
        };
    }

    private WeightDto ConvertWeightSample(HKQuantitySample sample)
    {
        var value = sample.Quantity.GetDoubleValue(HKUnit.Gram) / 1000.0; // Convert grams to kg
        var timestamp = new DateTimeOffset(sample.StartDate.ToDateTime());
        
        return new WeightDto
        {
            Id = sample.Uuid.ToString(),
            DataOrigin = sample.SourceRevision?.Source?.Name ?? "Unknown",
            Timestamp = timestamp,
            Value = value,
            Unit = "kg"
        };
    }

    private HeightDto ConvertHeightSample(HKQuantitySample sample)
    {
        var valueInMeters = sample.Quantity.GetDoubleValue(HKUnit.Meter);
        var timestamp = new DateTimeOffset(sample.StartDate.ToDateTime());
        
        return new HeightDto
        {
            Id = sample.Uuid.ToString(),
            DataOrigin = sample.SourceRevision?.Source?.Name ?? "Unknown",
            Timestamp = timestamp,
            Value = valueInMeters * 100, // Convert to cm
            Unit = "cm"
        };
    }

    /// <summary>
    /// <param name="canRequestFullHistoryPermission">iOS has this by default as TRUE</param>
    /// <returns></returns>
    public async partial Task<RequestPermissionResult> RequestPermissions(
        IList<HealthPermissionDto> healthPermissions,
        bool canRequestFullHistoryPermission,
        CancellationToken cancellationToken)
    {
        if (!IsSupported)
        {
            return new RequestPermissionResult()
            {
                Error = Enums.Errors.RequestPermissionError.IsNotSupported
            };
        }

        var typesToRead = healthPermissions
            .Where(x => x.PermissionType.HasFlag(PermissionType.Read))
            .Select(healthPermission => HKQuantityType.Create(
                healthPermission.HealthDataType.ToHKQuantityTypeIdentifier())!
            )
            .ToArray();
        var nsTypesToRead = new NSSet<HKQuantityType>(typesToRead);

        var typesToWrite = healthPermissions
            .Where(x => x.PermissionType.HasFlag(PermissionType.Write))
            .Select(healthPermission => HKQuantityType.Create(
                healthPermission.HealthDataType.ToHKQuantityTypeIdentifier())!
            )
            .ToArray();
        var nsTypesToWrite = new NSSet<HKQuantityType>(typesToWrite);

        try
        {
            using var healthStore = new HKHealthStore();


            //https://developer.apple.com/documentation/healthkit/hkhealthstore/1614152-requestauthorization
            var (isSuccess, error) = await healthStore.RequestAuthorizationToShareAsync(nsTypesToWrite, nsTypesToRead);
            if (!isSuccess)
            {
                return new RequestPermissionResult()
                {
                    Error = RequestPermissionError.ProblemWhileGrantingPermissions
                };
            }

            //https://developer.apple.com/documentation/healthkit/hkhealthstore/1614154-authorizationstatus#discussion
            //To help prevent possible leaks of sensitive health information, your app cannot determine whether or not
            //a user has granted permission to read data.If you are not given permission, it simply appears as if there
            //is no data of the requested type in the HealthKit store.If your app is given share permission but not read
            //permission, you see only the data that your app has written to the store.Data from other sources remains
            //hidden.

            if (typesToWrite.Any())
            {
                foreach (var typeToWrite in typesToWrite)
                {
                    var status = healthStore.GetAuthorizationStatus(typeToWrite);
                    if (status != HKAuthorizationStatus.SharingAuthorized)
                    {
                        return new RequestPermissionResult()
                        {
                            Error = RequestPermissionError.MissingPermissions
                        };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return new RequestPermissionResult()
            {
                Error = RequestPermissionError.ProblemWhileGrantingPermissions,
                ErrorException = ex
            };
        }
        return new();
    }
}
