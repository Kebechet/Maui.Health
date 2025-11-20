using Foundation;
using HealthKit;
using Maui.Health.Enums;
using Maui.Health.Enums.Errors;
using Maui.Health.Extensions;
using Maui.Health.Models;
using Maui.Health.Models.Metrics;
using Maui.Health.Platforms.iOS.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Platform;
using System.Collections.Generic;
using static Maui.Health.Constants.HealthConstants;

namespace Maui.Health.Services;

public partial class HealthService
{
    public partial bool IsSupported => HKHealthStore.IsHealthDataAvailable;
    private nuint _healthRateLimit { get; set; } = Defaults.HealthRateLimit;

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

    public async partial Task<List<TDto>> GetHealthData<TDto>(HealthTimeRange timeRange, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        if (!IsSupported)
        {
            return [];
        }

        try
        {
            _logger.LogInformation("iOS GetHealthDataAsync<{DtoName}>: StartTime: {StartTime} (Local: {StartDateTime}), EndTime: {EndTime} (Local: {EndDateTime})",
                typeof(TDto).Name, timeRange.StartTime, timeRange.StartDateTime, timeRange.EndTime, timeRange.EndDateTime);

            // Request permission for the specific metric
            var permission = MetricDtoExtensions.GetRequiredPermission<TDto>();
            var permissionResult = await RequestPermissions([permission], cancellationToken: cancellationToken);
            if (!permissionResult.IsSuccess)
            {
                return [];
            }

            var healthDataType = MetricDtoExtensions.GetHealthDataType<TDto>();

            // Special handling for BloodPressureDto - split into systolic/diastolic on iOS
            //if (typeof(TDto) == typeof(BloodPressureDto))
            //{
            //    return await GetBloodPressureAsync<TDto>(from, to, cancellationToken);
            //}

            var quantityType = HKQuantityType.Create(healthDataType.ToHKQuantityTypeIdentifier())!;

            var predicate = HKQuery.GetPredicateForSamples(
                timeRange.StartTime.ToNSDate(),
                timeRange.EndTime.ToNSDate(),
                HKQueryOptions.StrictStartDate
            );

            // Use HKStatisticsQuery for cumulative types (steps, calories, distance) to deduplicate
            if (IsCumulativeType<TDto>())
            {
                return await GetCumulativeHealthDataAsync<TDto>(quantityType, predicate, timeRange, healthDataType, cancellationToken);
            }

            var tcs = new TaskCompletionSource<TDto[]>();

            // Use HKSampleQuery to get individual records (for non-cumulative types like heart rate, weight)
            var query = new HKSampleQuery(
                quantityType,
                predicate,
                _healthRateLimit,
                [new NSSortDescriptor(HKSample.SortIdentifierStartDate, false)],
                (sampleQuery, results, error) =>
                {
                    if (error != null)
                    {
                        tcs.TrySetResult([]);
                        return;
                    }

                    var dtos = new List<TDto>();
                    foreach (var sample in results?.OfType<HKQuantitySample>() ?? [])
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
            _logger.LogInformation("Found {Count} {DtoName} records", results.Length, typeof(TDto).Name);

            return results.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching health data for {DtoName}", typeof(TDto).Name);
            return [];
        }
    }

    private static bool IsCumulativeType<TDto>() where TDto : HealthMetricBase
    {
        return typeof(TDto) == typeof(StepsDto) ||
               typeof(TDto) == typeof(ActiveCaloriesBurnedDto);
    }

    private async Task<List<TDto>> GetCumulativeHealthDataAsync<TDto>(
        HKQuantityType quantityType,
        NSPredicate predicate,
        HealthTimeRange timeRange,
        HealthDataType healthDataType,
        CancellationToken cancellationToken) where TDto : HealthMetricBase
    {
        var tcs = new TaskCompletionSource<TDto[]>();

        var query = new HKStatisticsQuery(
            quantityType,
            predicate,
            HKStatisticsOptions.CumulativeSum,
            (statisticsQuery, statistics, error) =>
            {
                if (error != null || statistics == null)
                {
                    tcs.TrySetResult([]);
                    return;
                }

                var sum = statistics.SumQuantity();
                if (sum == null)
                {
                    tcs.TrySetResult([]);
                    return;
                }

                TDto? dto = null;

                if (typeof(TDto) == typeof(StepsDto))
                {
                    dto = new StepsDto
                    {
                        Id = Guid.NewGuid().ToString(),
                        DataOrigin = DataOrigins.HealthKit,
                        Timestamp = timeRange.StartTime,
                        Count = (long)sum.GetDoubleValue(HKUnit.Count),
                        StartTime = timeRange.StartTime,
                        EndTime = timeRange.EndTime
                    } as TDto;
                }
                else if (typeof(TDto) == typeof(ActiveCaloriesBurnedDto))
                {
                    dto = new ActiveCaloriesBurnedDto
                    {
                        Id = Guid.NewGuid().ToString(),
                        DataOrigin = DataOrigins.HealthKit,
                        Timestamp = timeRange.StartTime,
                        Energy = sum.GetDoubleValue(HKUnit.Kilocalorie),
                        StartTime = timeRange.StartTime,
                        EndTime = timeRange.EndTime,
                        Unit = Units.Kilocalorie
                    } as TDto;
                }

                tcs.TrySetResult(dto != null ? [dto] : []);
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

        if (results.Length > 0)
        {
            _logger.LogInformation("Found cumulative {DtoName}: {Value}", typeof(TDto).Name,
                results[0] is StepsDto steps ? steps.Count.ToString() :
                results[0] is ActiveCaloriesBurnedDto cal ? cal.Energy.ToString("F0") : "N/A");
        }

        return results.ToList();
    }

    public async partial Task<bool> WriteHealthData<TDto>(TDto data, CancellationToken cancellationToken) where TDto : HealthMetricBase
    {
        if (!IsSupported)
        {
            return false;
        }

        try
        {
            _logger.LogInformation("iOS WriteHealthDataAsync<{DtoName}>", typeof(TDto).Name);

            // Request write permission for the specific metric
            var readPermission = MetricDtoExtensions.GetRequiredPermission<TDto>();
            var writePermission = new HealthPermissionDto
            {
                HealthDataType = readPermission.HealthDataType,
                PermissionType = PermissionType.Write
            };
            var permissionResult = await RequestPermissions([writePermission], cancellationToken: cancellationToken);
            if (!permissionResult.IsSuccess)
            {
                _logger.LogWarning("iOS Write: Permission denied for {DtoName}", typeof(TDto).Name);
                return false;
            }

            // Convert DTO to HKObject (HKQuantitySample or HKWorkout)
            HKObject? sample = data.ToHKObject();
            if (sample == null)
            {
                _logger.LogWarning("iOS Write: Failed to convert {DtoName} to HKObject", typeof(TDto).Name);
                return false;
            }

            // Save to HealthKit
            using var healthStore = new HKHealthStore();
            var tcs = new TaskCompletionSource<bool>();

            healthStore.SaveObject(sample, (success, error) =>
            {
                if (error != null)
                {
                    _logger.LogError("iOS Write Error: {Error}", error.LocalizedDescription);
                    tcs.TrySetResult(false);
                }
                else
                {
                    _logger.LogInformation("iOS Write: Successfully wrote {DtoName}", typeof(TDto).Name);
                    tcs.TrySetResult(success);
                }
            });

            using var ct = cancellationToken.Register(() => tcs.TrySetCanceled());
            return await tcs.Task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "iOS Write Exception for {DtoName}", typeof(TDto).Name);
            return false;
        }
    }

}