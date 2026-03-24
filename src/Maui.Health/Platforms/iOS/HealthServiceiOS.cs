using System.Text.Json;
using Foundation;
using HealthKit;
using Maui.Health.Constants;
using Maui.Health.Enums;
using Maui.Health.Enums.Errors;
using Maui.Health.Extensions;
using Maui.Health.Models;
using Maui.Health.Models.Metrics;
using Maui.Health.Platforms.iOS.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Platform;

namespace Maui.Health.Services;

public partial class HealthService : IHealthService
{
    public partial bool IsSupported => HKHealthStore.IsHealthDataAvailable;
    private nuint _healthRateLimit { get; set; } = Defaults.HeartRateLimit;

    /// <inheritdoc/>
    /// <remarks>
    /// iOS has full history permission by default (canRequestFullHistoryPermission is ignored).
    /// </remarks>
    public async partial Task<RequestPermissionResult> RequestPermissions(
        IList<HealthPermissionDto> healthPermissions,
        bool canRequestFullHistoryPermission,
        CancellationToken cancellationToken)
    {
        if (!IsSupported)
        {
            return new RequestPermissionResult()
            {
                Error = Enums.Errors.RequestPermissionError.SdkUnavailable
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

    //https://github.com/Kebechet/Maui.Health/pull/8/files
    //Split to `public partial` and `private async` method because of trimmer/linker issue
    public partial Task<List<TDto>> GetHealthData<TDto>(HealthTimeRange timeRange, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        return GetHealthDataInternal<TDto>(timeRange, cancellationToken);
    }

    private async Task<List<TDto>> GetHealthDataInternal<TDto>(HealthTimeRange timeRange, CancellationToken cancellationToken)
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
                        var dto = sample.ToDto<TDto>();
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

    //https://github.com/Kebechet/Maui.Health/pull/8/files
    //Split to `public partial` and `private async` method because of trimmer/linker issue
    public partial Task<bool> WriteHealthData<TDto>(TDto data, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        return WriteHealthDataInternal(data, cancellationToken);
    }

    private async Task<bool> WriteHealthDataInternal<TDto>(TDto data, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
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

    //https://github.com/Kebechet/Maui.Health/pull/8/files
    //Split to `public partial` and `private async` method because of trimmer/linker issue
    public partial Task<TDto?> GetHealthRecord<TDto>(string id, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        return GetHealthRecordInternal<TDto>(id, cancellationToken);
    }

    private async Task<TDto?> GetHealthRecordInternal<TDto>(string id, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        if (!IsSupported)
        {
            return null;
        }

        try
        {
            var permission = MetricDtoExtensions.GetRequiredPermission<TDto>();
            var permissionResult = await RequestPermissions([permission], cancellationToken: cancellationToken);
            if (!permissionResult.IsSuccess)
            {
                return null;
            }

            if (!Guid.TryParse(id, out var guid))
            {
                return null;
            }

            var healthDataType = MetricDtoExtensions.GetHealthDataType<TDto>();
            var quantityType = HKQuantityType.Create(healthDataType.ToHKQuantityTypeIdentifier())!;

            var uuid = new NSUuid(guid.ToString());
            var predicate = HKQuery.GetPredicateForObject(uuid);

            var tcs = new TaskCompletionSource<TDto?>();

            var query = new HKSampleQuery(
                quantityType,
                predicate,
                1,
                null,
                (_, results, error) =>
                {
                    if (error is not null || results is null)
                    {
                        tcs.TrySetResult(null);
                        return;
                    }

                    var sample = results.FirstOrDefault() as HKQuantitySample;
                    tcs.TrySetResult(sample?.ToDto<TDto>());
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching health record {Id} for {DtoName}", id, typeof(TDto).Name);
            return null;
        }
    }

    //https://github.com/Kebechet/Maui.Health/pull/8/files
    //Split to `public partial` and `private async` method because of trimmer/linker issue
    public partial Task<bool> DeleteHealthData<TDto>(string id, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        return DeleteHealthDataInternal<TDto>(id, cancellationToken);
    }

    private async Task<bool> DeleteHealthDataInternal<TDto>(string id, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        if (!IsSupported)
        {
            return false;
        }

        try
        {
            var readPermission = MetricDtoExtensions.GetRequiredPermission<TDto>();
            var writePermission = new HealthPermissionDto
            {
                HealthDataType = readPermission.HealthDataType,
                PermissionType = PermissionType.Write
            };
            var permissionResult = await RequestPermissions([writePermission], cancellationToken: cancellationToken);
            if (!permissionResult.IsSuccess)
            {
                return false;
            }

            if (!Guid.TryParse(id, out var guid))
            {
                return false;
            }

            var healthDataType = MetricDtoExtensions.GetHealthDataType<TDto>();
            var quantityType = HKQuantityType.Create(healthDataType.ToHKQuantityTypeIdentifier())!;

            var uuid = new NSUuid(guid.ToString());
            var predicate = HKQuery.GetPredicateForObject(uuid);

            // First find the sample, then delete it
            var findTcs = new TaskCompletionSource<HKQuantitySample?>();

            var findQuery = new HKSampleQuery(
                quantityType,
                predicate,
                1,
                null,
                (_, results, error) =>
                {
                    findTcs.TrySetResult(results?.FirstOrDefault() as HKQuantitySample);
                }
            );

            using var store = new HKHealthStore();
            using var ct = cancellationToken.Register(() => findTcs.TrySetCanceled());

            store.ExecuteQuery(findQuery);
            var sample = await findTcs.Task;

            if (sample is null)
            {
                _logger.LogWarning("iOS Delete: Record {Id} not found for {DtoName}", id, typeof(TDto).Name);
                return false;
            }

            var isDeleted = await store.Delete(sample, cancellationToken);

            if (isDeleted)
            {
                _logger.LogInformation("iOS Delete: Successfully deleted {DtoName} record {Id}", typeof(TDto).Name, id);
            }

            return isDeleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "iOS Delete error for {DtoName} record {Id}", typeof(TDto).Name, id);
            return false;
        }
    }

    //https://github.com/Kebechet/Maui.Health/pull/8/files
    //Split to `public partial` and `private async` method because of trimmer/linker issue
    public partial Task<AggregatedResult?> GetAggregatedHealthData<TDto>(HealthTimeRange timeRange, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        return GetAggregatedHealthDataInternal<TDto>(timeRange, cancellationToken);
    }

    private async Task<AggregatedResult?> GetAggregatedHealthDataInternal<TDto>(HealthTimeRange timeRange, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        if (!IsSupported)
        {
            return null;
        }

        try
        {
            var permission = MetricDtoExtensions.GetRequiredPermission<TDto>();
            var permissionResult = await RequestPermissions([permission], cancellationToken: cancellationToken);
            if (!permissionResult.IsSuccess)
            {
                return null;
            }

            var healthDataType = MetricDtoExtensions.GetHealthDataType<TDto>();
            var quantityType = HKQuantityType.Create(healthDataType.ToHKQuantityTypeIdentifier())!;

            var predicate = HKQuery.GetPredicateForSamples(
                timeRange.StartTime.ToNSDate(),
                timeRange.EndTime.ToNSDate(),
                HKQueryOptions.StrictStartDate
            );

            var (statisticsOption, hkUnit) = GetStatisticsInfo(healthDataType);

            var tcs = new TaskCompletionSource<AggregatedResult?>();

            var query = new HKStatisticsQuery(
                quantityType,
                predicate,
                statisticsOption,
                (_, statistics, error) =>
                {
                    if (error is not null || statistics is null)
                    {
                        tcs.TrySetResult(null);
                        return;
                    }

                    var quantity = IsCumulativeType(healthDataType)
                        ? statistics.SumQuantity()
                        : statistics.AverageQuantity();

                    if (quantity is null)
                    {
                        tcs.TrySetResult(null);
                        return;
                    }

                    var value = quantity.GetDoubleValue(hkUnit);
                    var unit = GetUnitString(healthDataType);

                    tcs.TrySetResult(new AggregatedResult
                    {
                        StartTime = timeRange.StartTime,
                        EndTime = timeRange.EndTime,
                        Value = value,
                        Unit = unit,
                        DataType = healthDataType
                    });
                }
            );

            using var store = new HKHealthStore();
            using var ct = cancellationToken.Register(() =>
            {
                tcs.TrySetCanceled();
                store.StopQuery(query);
            });

            store.ExecuteQuery(query);
            var result = await tcs.Task;

            if (result is not null)
            {
                _logger.LogInformation("iOS Aggregated {DtoName}: {Value}", typeof(TDto).Name, result.Value);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aggregating health data for {DtoName}", typeof(TDto).Name);
            return null;
        }
    }

    //https://github.com/Kebechet/Maui.Health/pull/8/files
    //Split to `public partial` and `private async` method because of trimmer/linker issue
    public partial Task<List<AggregatedResult>> GetAggregatedHealthDataByInterval<TDto>(HealthTimeRange timeRange, TimeSpan interval, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        return GetAggregatedHealthDataByIntervalInternal<TDto>(timeRange, interval, cancellationToken);
    }

    private async Task<List<AggregatedResult>> GetAggregatedHealthDataByIntervalInternal<TDto>(HealthTimeRange timeRange, TimeSpan interval, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        if (!IsSupported)
        {
            return [];
        }

        try
        {
            var permission = MetricDtoExtensions.GetRequiredPermission<TDto>();
            var permissionResult = await RequestPermissions([permission], cancellationToken: cancellationToken);
            if (!permissionResult.IsSuccess)
            {
                return [];
            }

            var healthDataType = MetricDtoExtensions.GetHealthDataType<TDto>();
            var quantityType = HKQuantityType.Create(healthDataType.ToHKQuantityTypeIdentifier())!;

            var predicate = HKQuery.GetPredicateForSamples(
                timeRange.StartTime.ToNSDate(),
                timeRange.EndTime.ToNSDate(),
                HKQueryOptions.StrictStartDate
            );

            var (statisticsOption, hkUnit) = GetStatisticsInfo(healthDataType);
            var unit = GetUnitString(healthDataType);
            var isCumulative = IsCumulativeType(healthDataType);

            var intervalComponents = new NSDateComponents();
            if (interval.TotalDays >= 1)
            {
                intervalComponents.Day = (nint)interval.TotalDays;
            }
            else if (interval.TotalHours >= 1)
            {
                intervalComponents.Hour = (nint)interval.TotalHours;
            }
            else
            {
                intervalComponents.Minute = (nint)interval.TotalMinutes;
            }

            var tcs = new TaskCompletionSource<List<AggregatedResult>>();

            var query = new HKStatisticsCollectionQuery(
                quantityType,
                predicate,
                statisticsOption,
                timeRange.StartTime.ToNSDate(),
                intervalComponents);

            query.InitialResultsHandler = (_, results, error) =>
            {
                if (error is not null || results is null)
                {
                    tcs.TrySetResult([]);
                    return;
                }

                var aggregatedResults = new List<AggregatedResult>();

                results.EnumerateStatistics(
                    timeRange.StartTime.ToNSDate(),
                    timeRange.EndTime.ToNSDate(),
                    (statistics, _) =>
                    {
                        var quantity = isCumulative
                            ? statistics.SumQuantity()
                            : statistics.AverageQuantity();

                        if (quantity is null)
                        {
                            return;
                        }

                        var value = quantity.GetDoubleValue(hkUnit);
                        var bucketStart = statistics.StartDate.ToDateTimeOffset();
                        var bucketEnd = statistics.EndDate.ToDateTimeOffset();

                        aggregatedResults.Add(new AggregatedResult
                        {
                            StartTime = bucketStart,
                            EndTime = bucketEnd,
                            Value = value,
                            Unit = unit,
                            DataType = healthDataType
                        });
                    });

                tcs.TrySetResult(aggregatedResults);
            };

            using var store = new HKHealthStore();
            using var ct = cancellationToken.Register(() =>
            {
                tcs.TrySetCanceled();
                store.StopQuery(query);
            });

            store.ExecuteQuery(query);
            var result = await tcs.Task;

            _logger.LogInformation("Found {Count} interval buckets for {DtoName}", result.Count, typeof(TDto).Name);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aggregating health data by interval for {DtoName}", typeof(TDto).Name);
            return [];
        }
    }

    public async partial Task<string?> GetChangesToken(IList<HealthDataType> dataTypes, CancellationToken cancellationToken)
    {
        if (!IsSupported)
        {
            return null;
        }

        try
        {
            // iOS uses HKAnchoredObjectQuery with anchors instead of tokens.
            // We serialize the initial anchor (HKQueryAnchor.Create(0)) as a token string.
            // The "token" is a JSON object containing the anchor value and the data types.
            var tokenData = new
            {
                Anchor = 0,
                DataTypes = dataTypes.Select(dt => dt.ToString()).ToList()
            };

            return JsonSerializer.Serialize(tokenData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating changes token");
            return null;
        }
    }

    public async partial Task<HealthChangesResult?> GetChanges(string token, CancellationToken cancellationToken)
    {
        if (!IsSupported)
        {
            return null;
        }

        try
        {
            var tokenData = JsonSerializer.Deserialize<JsonElement>(token);
            var anchorValue = tokenData.GetProperty("Anchor").GetInt64();
            var dataTypeStrings = tokenData.GetProperty("DataTypes").EnumerateArray()
                .Select(dt => dt.GetString())
                .Where(dt => dt is not null)
                .ToList();

            var anchor = anchorValue > 0
                ? HKQueryAnchor.Create((nuint)anchorValue)
                : HKQueryAnchor.Create(0);

            var allChanges = new List<HealthChange>();
            nuint latestAnchorValue = (nuint)anchorValue;

            foreach (var dtString in dataTypeStrings)
            {
                if (!Enum.TryParse<HealthDataType>(dtString, out var healthDataType))
                {
                    continue;
                }

                if (healthDataType == HealthDataType.ExerciseSession)
                {
                    continue;
                }

                var quantityType = HKQuantityType.Create(healthDataType.ToHKQuantityTypeIdentifier());
                if (quantityType is null)
                {
                    continue;
                }

                var tcs = new TaskCompletionSource<(HKSample[]? Added, HKDeletedObject[]? Deleted, HKQueryAnchor? NewAnchor)>();

                var query = new HKAnchoredObjectQuery(
                    quantityType,
                    null,
                    anchor,
                    0,
                    (_, addedObjects, deletedObjects, newAnchor, error) =>
                    {
                        if (error is not null)
                        {
                            tcs.TrySetResult((null, null, null));
                            return;
                        }

                        tcs.TrySetResult((addedObjects, deletedObjects, newAnchor));
                    }
                );

                using var store = new HKHealthStore();
                using var ct = cancellationToken.Register(() =>
                {
                    tcs.TrySetCanceled();
                    store.StopQuery(query);
                });

                store.ExecuteQuery(query);
                var (added, deleted, newAnchor) = await tcs.Task;

                if (added is not null)
                {
                    foreach (var sample in added)
                    {
                        allChanges.Add(new HealthChange
                        {
                            Type = HealthChangeType.Upsert,
                            RecordId = sample.Uuid.ToString(),
                            DataType = healthDataType
                        });
                    }
                }

                if (deleted is not null)
                {
                    foreach (var deletedObj in deleted)
                    {
                        allChanges.Add(new HealthChange
                        {
                            Type = HealthChangeType.Deletion,
                            RecordId = deletedObj.Uuid.ToString()
                        });
                    }
                }

                if (newAnchor is not null)
                {
                    // Extract the anchor value via description string parsing since HKQueryAnchor
                    // doesn't expose its internal value directly
                    var anchorDesc = newAnchor.Description;
                    if (nuint.TryParse(anchorDesc, out var parsedAnchor))
                    {
                        if (parsedAnchor > latestAnchorValue)
                        {
                            latestAnchorValue = parsedAnchor;
                        }
                    }
                }
            }

            var nextTokenData = new
            {
                Anchor = (long)latestAnchorValue,
                DataTypes = dataTypeStrings
            };

            _logger.LogInformation("iOS GetChanges: {Count} changes", allChanges.Count);

            return new HealthChangesResult
            {
                Changes = allChanges,
                NextToken = JsonSerializer.Serialize(nextTokenData),
                HasMore = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting health changes");
            return null;
        }
    }

    private static bool IsCumulativeType<TDto>() where TDto : HealthMetricBase
    {
        return typeof(TDto) == typeof(StepsDto) ||
               typeof(TDto) == typeof(ActiveCaloriesBurnedDto);
    }

    private static bool IsCumulativeType(HealthDataType healthDataType)
    {
        return healthDataType is HealthDataType.Steps
            or HealthDataType.ActiveCaloriesBurned
            or HealthDataType.Hydration;
    }

    private static (HKStatisticsOptions Option, HKUnit Unit) GetStatisticsInfo(HealthDataType healthDataType)
    {
        return healthDataType switch
        {
            HealthDataType.Steps => (HKStatisticsOptions.CumulativeSum, HKUnit.Count),
            HealthDataType.ActiveCaloriesBurned => (HKStatisticsOptions.CumulativeSum, HKUnit.Kilocalorie),
            HealthDataType.Hydration => (HKStatisticsOptions.CumulativeSum, HKUnit.Liter),
            HealthDataType.Weight => (HKStatisticsOptions.DiscreteAverage, HKUnit.FromString("kg")),
            HealthDataType.HeartRate => (HKStatisticsOptions.DiscreteAverage, HKUnit.FromString("count/min")),
            HealthDataType.Height => (HKStatisticsOptions.DiscreteAverage, HKUnit.FromString("cm")),
            _ => (HKStatisticsOptions.DiscreteAverage, HKUnit.Count)
        };
    }

    private static string? GetUnitString(HealthDataType healthDataType)
    {
        return healthDataType switch
        {
            HealthDataType.Steps => null,
            HealthDataType.ActiveCaloriesBurned => Units.Kilocalorie,
            HealthDataType.Hydration => Units.Liter,
            HealthDataType.Weight => Units.Kilogram,
            HealthDataType.HeartRate => Units.BeatsPerMinute,
            HealthDataType.Height => Units.Centimeter,
            _ => null
        };
    }

    public partial void OpenStorePageOfHealthProvider() { }

    private async Task<List<TDto>> GetCumulativeHealthDataAsync<TDto>(
        HKQuantityType quantityType,
        NSPredicate predicate,
        HealthTimeRange timeRange,
        HealthDataType healthDataType,
        CancellationToken cancellationToken)
        where TDto : HealthMetricBase
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
                        DataOrigin = DataOrigin.HealthKitOrigin,
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
                        DataOrigin = DataOrigin.HealthKitOrigin,
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
}