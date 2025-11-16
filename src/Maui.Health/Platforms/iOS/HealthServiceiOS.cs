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
    private nuint _healthRateLimit { get; set; } = 0;

    public async partial Task<List<TDto>> GetHealthDataAsync<TDto>(HealthTimeRange timeRange, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        if (!IsSupported)
        {
            return [];
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"iOS GetHealthDataAsync<{typeof(TDto).Name}>:");
            System.Diagnostics.Debug.WriteLine($"  StartTime: {timeRange.StartTime} (Local: {timeRange.StartDateTime})");
            System.Diagnostics.Debug.WriteLine($"  EndTime: {timeRange.EndTime} (Local: {timeRange.EndDateTime})");

            // Request permission for the specific metric
            var permission = MetricDtoExtensions.GetRequiredPermission<TDto>();
            var permissionResult = await RequestPermissions([permission], cancellationToken: cancellationToken);
            if (!permissionResult.IsSuccess)
            {
                return [];
            }

            var healthDataType = MetricDtoExtensions.GetHealthDataType<TDto>();

            // Special handling for WorkoutDto - uses HKWorkout instead of HKQuantitySample
            if (typeof(TDto) == typeof(WorkoutDto))
            {
                return await GetWorkoutsAsync<TDto>(timeRange, cancellationToken);
            }

            // Special handling for BloodPressureDto - split into systolic/diastolic on iOS
            //if (typeof(TDto) == typeof(BloodPressureDto))
            //{
            //    return await GetBloodPressureAsync<TDto>(from, to, cancellationToken);
            //}

            var quantityType = HKQuantityType.Create(healthDataType.ToHKQuantityTypeIdentifier())!;

            var predicate = HKQuery.GetPredicateForSamples(
                (NSDate)timeRange.StartDateTime,
                (NSDate)timeRange.EndDateTime,
                HKQueryOptions.StrictStartDate
            );

            var tcs = new TaskCompletionSource<TDto[]>();

            // Use HKSampleQuery to get individual records
            var query = new HKSampleQuery(
                quantityType,
                predicate,
                _healthRateLimit, // No limit
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
                        var dto = sample.ConvertToDto<TDto>(healthDataType);
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
            var results = await tcs.Task;
            System.Diagnostics.Debug.WriteLine($"  Found {results.Length} {typeof(TDto).Name} records");
            return results.ToList();
        }
        catch (Exception)
        {
            return [];
        }
    }

    private async Task<List<TDto>> GetWorkoutsAsync<TDto>(HealthTimeRange timeRange, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        var predicate = HKQuery.GetPredicateForSamples(
            (NSDate)timeRange.StartDateTime,
            (NSDate)timeRange.EndDateTime,
            HKQueryOptions.StrictStartDate
        );

        var tcs = new TaskCompletionSource<HKWorkout[]>();
        var workoutType = HKWorkoutType.WorkoutType;

        var query = new HKSampleQuery(
            workoutType,
            predicate,
            _healthRateLimit, // No limit
            new[] { new NSSortDescriptor(HKSample.SortIdentifierStartDate, false) },
            (HKSampleQuery sampleQuery, HKSample[] results, NSError error) =>
            {
                if (error != null)
                {
                    tcs.TrySetResult([]);
                    return;
                }

                var workouts = results.OfType<HKWorkout>().ToArray();
                tcs.TrySetResult(workouts);
            }
        );

        using var store = new HKHealthStore();
        using var ct = cancellationToken.Register(() =>
        {
            tcs.TrySetCanceled();
            store.StopQuery(query);
        });

        store.ExecuteQuery(query);
        var workouts = await tcs.Task;

        // Now fetch heart rate data for each workout and convert to DTOs
        var dtos = new List<TDto>();
        foreach (var workout in workouts)
        {
            var dto = await workout.ToWorkoutDtoAsync(QueryHeartRateSamplesAsync, cancellationToken) as TDto;
            if (dto is not null)
            {
                dtos.Add(dto);
            }
        }

        return dtos;
    }

    private async Task<HeartRateDto[]> QueryHeartRateSamplesAsync(HealthTimeRange timeRange, CancellationToken cancellationToken)
    {
        // Ensure DateTime is treated as UTC for NSDate conversion
        var fromUtc = DateTime.SpecifyKind(timeRange.StartDateTime, DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(timeRange.EndDateTime, DateTimeKind.Utc);

        var quantityType = HKQuantityType.Create(HKQuantityTypeIdentifier.HeartRate)!;
        var predicate = HKQuery.GetPredicateForSamples((NSDate)fromUtc, (NSDate)toUtc, HKQueryOptions.StrictStartDate);
        var tcs = new TaskCompletionSource<HeartRateDto[]>();

        var query = new HKSampleQuery(
            quantityType,
            predicate,
            _healthRateLimit,
            new[] { new NSSortDescriptor(HKSample.SortIdentifierStartDate, false) },
            (HKSampleQuery sampleQuery, HKSample[] results, NSError error) =>
            {
                if (error != null)
                {
                    tcs.TrySetResult([]);
                    return;
                }

                var dtos = new List<HeartRateDto>();
                foreach (var sample in results.OfType<HKQuantitySample>())
                {
                    var dto = sample.ToHeartRateDto();
                    dtos.Add(dto);
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

        var readTypes = new List<HKObjectType>();
        var writeTypes = new List<HKObjectType>();

        foreach (var permission in healthPermissions)
        {
            HKObjectType? type = null;

            // Special handling for workout/exercise session
            if (permission.HealthDataType == HealthDataType.ExerciseSession)
            {
                type = HKWorkoutType.WorkoutType;
            }
            else
            {
                type = HKQuantityType.Create(permission.HealthDataType.ToHKQuantityTypeIdentifier());
            }

            if (type != null)
            {
                if (permission.PermissionType.HasFlag(PermissionType.Read))
                {
                    readTypes.Add(type);
                }
                if (permission.PermissionType.HasFlag(PermissionType.Write))
                {
                    writeTypes.Add(type);
                }
            }
        }

        var nsTypesToRead = new NSSet<HKObjectType>(readTypes.ToArray());
        var nsTypesToWrite = new NSSet<HKObjectType>(writeTypes.ToArray());

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

            if (writeTypes.Any())
            {
                foreach (var typeToWrite in writeTypes)
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
