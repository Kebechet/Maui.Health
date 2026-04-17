using Android.Content;
using AndroidX.Activity;
using AndroidX.Activity.Result;
using AndroidX.Health.Connect.Client;
using Java.Util;
using Maui.Health.Constants;
using Maui.Health.Enums;
using Maui.Health.Enums.Errors;
using Maui.Health.Extensions;
using Maui.Health.Models;
using Maui.Health.Models.Metrics;
using Maui.Health.Platforms.Android;
using Maui.Health.Platforms.Android.Callbacks;
using Maui.Health.Platforms.Android.Extensions;
using Maui.Health.Platforms.Android.Helpers;
using Microsoft.Extensions.Logging;
using static Maui.Health.Platforms.Android.AndroidConstant;

namespace Maui.Health.Services;

public partial class HealthService : IHealthService
{
    private Result<SdkStatus> _sdkStatus => _activityContext.CheckSdkAvailability();
    public partial bool IsSupported => _sdkStatus.IsSuccess;

    private Context _activityContext => Platform.CurrentActivity ??
        throw new Exception("Current activity is null");

    private IHealthConnectClient _healthConnectClient => HealthConnectClient.GetOrCreate(_activityContext);

    public async partial Task<RequestPermissionResult> RequestPermissions(IList<HealthPermissionDto> healthPermissions, bool canRequestFullHistoryPermission, CancellationToken cancellationToken)
    {
        try
        {
            var sdkCheckResult = _sdkStatus;
            if (!sdkCheckResult.IsSuccess)
            {
                if (sdkCheckResult.Error == SdkStatus.SdkUnavailableProviderUpdateRequired)
                {
                    return new()
                    {
                        Error = RequestPermissionError.SdkUnavailableProviderUpdateRequired
                    };
                }

                return new()
                {
                    Error = RequestPermissionError.SdkUnavailable
                };
            }

            var permissionsToGrant = healthPermissions
                .SelectMany(healthPermission => healthPermission.ToStrings())
                .ToList();

            if (canRequestFullHistoryPermission)
            {
                //https://developer.android.com/health-and-fitness/guides/health-connect/plan/data-types#alpha10
                permissionsToGrant.Add(AndroidConstant.FullHistoryReadPermission);
            }

            var grantedPermissions = await KotlinResolver.ProcessList<Java.Lang.String>(_healthConnectClient.PermissionController.GetGrantedPermissions);
            if (grantedPermissions is null)
            {
                return new()
                {
                    Error = RequestPermissionError.ProblemWhileFetchingAlreadyGrantedPermissions
                };
            }

            // Capture pre-call state to detect whether this request is a FRESH grant
            // (no health permission held beforehand). The single-anchor first-grant timestamp
            // is only set when we observe that transition, matching Health Connect's
            // "30 days prior to when any permission was first granted" rule:
            // https://developer.android.com/health-and-fitness/guides/health-connect/develop/read-data#read-data-older-than-30-days
            var hadAnyHealthPermissionBefore = grantedPermissions
                .Any(p => p?.ToString()?.StartsWith(AndroidConstant.HealthPermissionPrefix, StringComparison.Ordinal) == true);

            var missingPermissions = permissionsToGrant
                .Where(permission => !grantedPermissions.ToList().Contains(permission))
                .ToList();

            if (!missingPermissions.Any())
            {
                return new();
            }

            var key = Guid.NewGuid().ToString();
            var requestPermissionActivityContract = PermissionController.CreateRequestPermissionResultContract();
            var callback = new AndroidActivityResultCallback<ISet?>(cancellationToken);

            ActivityResultLauncher? launcher = null;
            ISet? newlyGrantedPermissions = null;
            ActivityResultRegistry? activityResultRegistry = null;
            try
            {
                activityResultRegistry = ((ComponentActivity)_activityContext).ActivityResultRegistry;
                launcher = activityResultRegistry.Register(key, requestPermissionActivityContract, callback);
                launcher.Launch(new HashSet(missingPermissions));

                newlyGrantedPermissions = await callback.Task;
            }
            finally
            {
                launcher?.Unregister();
            }

            var stillMissingPermissions = newlyGrantedPermissions is null
                ? missingPermissions
                : missingPermissions
                    .Where(permission => !newlyGrantedPermissions.ToList().Contains(permission))
                    .ToList();

            // If we transitioned from "no health permission" to "at least one granted",
            // record the first-grant anchor. The platform doesn't expose this value and
            // it drives the 30-day historical read window for all subsequent reads.
            // Runs BEFORE the partial-grant early-return below — the platform's anchor is
            // set by any successful grant, even if our caller didn't get everything asked.
            // Spec: https://developer.android.com/health-and-fitness/guides/health-connect/develop/read-data#read-data-older-than-30-days
            if (!hadAnyHealthPermissionBefore
                && newlyGrantedPermissions?
                    .ToList()
                    .Any(p => p?.ToString()?.StartsWith(AndroidConstant.HealthPermissionPrefix, StringComparison.Ordinal) == true) == true)
            {
                Preferences.Default.Set(AndroidConstant.FirstPermissionGrantAtKey, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }

            if (stillMissingPermissions.Any())
            {
                return new()
                {
                    Error = RequestPermissionError.MissingPermissions,
                    DeniedPermissions = stillMissingPermissions
                };
            }

            return new();
        }
        catch (Exception ex)
        {
            return new()
            {
                ErrorException = ex
            };
        }
    }

    public async partial Task<IList<HealthPermissionStatusResult>> GetPermissionStatuses(IList<HealthPermissionDto> permissions, CancellationToken cancellationToken)
    {
        if (!IsSupported)
        {
            return permissions
                .Select(p => new HealthPermissionStatusResult
                {
                    Permission = p,
                    Status = HealthPermissionStatus.NotDetermined
                })
                .ToList();
        }

        var grantedPermissions = await KotlinResolver.ProcessList<Java.Lang.String>(_healthConnectClient.PermissionController.GetGrantedPermissions);

        if (grantedPermissions is null)
        {
            return permissions
                .Select(p => new HealthPermissionStatusResult
                {
                    Permission = p,
                    Status = HealthPermissionStatus.NotDetermined
                })
                .ToList();
        }

        var grantedSet = grantedPermissions
            .Select(p => p?.ToString())
            .Where(p => p is not null)
            .ToHashSet();

        var results = new List<HealthPermissionStatusResult>();

        foreach (var permission in permissions)
        {
            var androidStrings = permission.ToStrings();

            foreach (var androidPermission in androidStrings)
            {
                var status = grantedSet.Contains(androidPermission)
                    ? HealthPermissionStatus.Granted
                    : HealthPermissionStatus.Denied;

                var individualPermission = new HealthPermissionDto
                {
                    HealthDataType = permission.HealthDataType,
                    PermissionType = androidPermission.Contains("READ_") ? PermissionType.Read : PermissionType.Write
                };

                results.Add(new HealthPermissionStatusResult
                {
                    Permission = individualPermission,
                    Status = status
                });
            }
        }

        return results;
    }

    //https://github.com/Kebechet/Maui.Health/pull/8/files
    //Split to `public partial` and `private async` method because of trimmer/linker issue
    public partial Task<List<TDto>> GetHealthData<TDto>(HealthTimeRange timeRange, bool shouldCheckPermissions, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        return GetHealthDataInternal<TDto>(timeRange, shouldCheckPermissions, cancellationToken);
    }

    private async Task<List<TDto>> GetHealthDataInternal<TDto>(HealthTimeRange timeRange, bool shouldCheckPermissions, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        try
        {
            _logger.LogInformation("Android GetHealthDataAsync<{DtoName}>: StartTime: {StartTime}, EndTime: {EndTime}",
                typeof(TDto).Name, timeRange.StartTime, timeRange.EndTime);

            var sdkCheckResult = _sdkStatus;
            if (!sdkCheckResult.IsSuccess)
            {
                return [];
            }

            if (shouldCheckPermissions)
            {
                var permission = MetricDtoExtensions.GetRequiredPermission<TDto>();
                var requestPermissionResult = await RequestPermissions([permission], false, cancellationToken);
                if (requestPermissionResult.IsError)
                {
                    return [];
                }
            }

            var healthDataType = MetricDtoExtensions.GetHealthDataType<TDto>();
            var recordClass = healthDataType.ToKotlinClass();

            var response = await _healthConnectClient.ReadHealthRecords(recordClass, timeRange);
            if (response is null)
            {
                return [];
            }

            var results = response.Records.ToDtoList<TDto>();

            _logger.LogInformation("Found {Count} {DtoName} records", results.Count, typeof(TDto).Name);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching health data for {DtoName}", typeof(TDto).Name);
            return [];
        }
    }

    //https://github.com/Kebechet/Maui.Health/pull/8/files
    //Split to `public partial` and `private async` method because of trimmer/linker issue
    public partial Task<bool> WriteHealthData<TDto>(IList<TDto> items, bool shouldCheckPermissions, CancellationToken cancellationToken)
        where TDto : IHealthWritable
    {
        return WriteHealthDataInternal(items, shouldCheckPermissions, cancellationToken);
    }

    private async Task<bool> WriteHealthDataInternal<TDto>(IList<TDto> items, bool shouldCheckPermissions, CancellationToken cancellationToken) where TDto : IHealthWritable
    {
        try
        {
            var sdkCheckResult = _sdkStatus;
            if (!sdkCheckResult.IsSuccess)
            {
                return false;
            }

            if (shouldCheckPermissions)
            {
                var writePermission = MetricDtoExtensions.GetRequiredWritePermission<TDto>();
                var requestPermissionResult = await RequestPermissions([writePermission], false, cancellationToken);
                if (requestPermissionResult.IsError)
                {
                    return false;
                }
            }

            var records = new List<Java.Lang.Object>();
            foreach (var item in items)
            {
                var record = item.ToAndroidRecord();
                if (record is null)
                {
                    _logger.LogWarning("Failed to convert {DtoName} to Android record", typeof(TDto).Name);
                    return false;
                }

                records.Add(record);
            }

            var success = await _healthConnectClient.InsertRecords(records);
            if (!success)
            {
                _logger.LogWarning("Failed to insert {Count} {DtoName} records", records.Count, typeof(TDto).Name);
                return false;
            }

            _logger.LogInformation("Successfully wrote {Count} {DtoName} records", records.Count, typeof(TDto).Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing health data for {DtoName}", typeof(TDto).Name);
            return false;
        }
    }

    //https://github.com/Kebechet/Maui.Health/pull/8/files
    //Split to `public partial` and `private async` method because of trimmer/linker issue
    public partial Task<TDto?> GetHealthRecord<TDto>(string id, bool shouldCheckPermissions, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        return GetHealthRecordInternal<TDto>(id, shouldCheckPermissions, cancellationToken);
    }

    private async Task<TDto?> GetHealthRecordInternal<TDto>(string id, bool shouldCheckPermissions, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        try
        {
            if (!_sdkStatus.IsSuccess)
            {
                return null;
            }

            if (shouldCheckPermissions)
            {
                var permission = MetricDtoExtensions.GetRequiredPermission<TDto>();
                var requestPermissionResult = await RequestPermissions([permission], false, cancellationToken);
                if (requestPermissionResult.IsError)
                {
                    return null;
                }
            }

            var healthDataType = MetricDtoExtensions.GetHealthDataType<TDto>();
            var recordClass = healthDataType.ToKotlinClass();

            var record = await _healthConnectClient.ReadHealthRecord(recordClass, id);
            if (record is null)
            {
                return null;
            }

            return record.ToDto<TDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching health record {Id} for {DtoName}", id, typeof(TDto).Name);
            return null;
        }
    }

    //https://github.com/Kebechet/Maui.Health/pull/8/files
    //Split to `public partial` and `private async` method because of trimmer/linker issue
    public partial Task<bool> DeleteHealthData<TDto>(string id, bool shouldCheckPermissions, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        return DeleteHealthDataInternal<TDto>(id, shouldCheckPermissions, cancellationToken);
    }

    private async Task<bool> DeleteHealthDataInternal<TDto>(string id, bool shouldCheckPermissions, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        try
        {
            if (!_sdkStatus.IsSuccess)
            {
                return false;
            }

            if (shouldCheckPermissions)
            {
                var readPermission = MetricDtoExtensions.GetRequiredPermission<TDto>();
                var writePermission = new HealthPermissionDto
                {
                    HealthDataType = readPermission.HealthDataType,
                    PermissionType = PermissionType.Write
                };
                var requestPermissionResult = await RequestPermissions([writePermission], false, cancellationToken);
                if (requestPermissionResult.IsError)
                {
                    return false;
                }
            }

            var healthDataType = MetricDtoExtensions.GetHealthDataType<TDto>();
            var recordClass = healthDataType.ToKotlinClass();

            var isDeleted = await _healthConnectClient.DeleteRecord(recordClass, id);

            if (isDeleted)
            {
                _logger.LogInformation("Successfully deleted {DtoName} record {Id}", typeof(TDto).Name, id);
            }
            else
            {
                _logger.LogWarning("Failed to delete {DtoName} record {Id}", typeof(TDto).Name, id);
            }

            return isDeleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting {DtoName} record {Id}", typeof(TDto).Name, id);
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
        try
        {
            if (!_sdkStatus.IsSuccess)
            {
                return null;
            }

            var permission = MetricDtoExtensions.GetRequiredPermission<TDto>();
            var requestPermissionResult = await RequestPermissions([permission], false, cancellationToken);
            if (requestPermissionResult.IsError)
            {
                return null;
            }

            var healthDataType = MetricDtoExtensions.GetHealthDataType<TDto>();
            var (recordClassName, metricFieldName, unit) = GetAggregateMetricInfo(healthDataType);
            if (recordClassName is null || metricFieldName is null)
            {
                _logger.LogWarning("Aggregation not supported for {DtoName}", typeof(TDto).Name);
                return null;
            }

            var (result, dataOrigins) = await _healthConnectClient.AggregateHealthRecords(recordClassName, metricFieldName, timeRange);
            if (result is null)
            {
                _logger.LogInformation("No aggregate data found for {DtoName}", typeof(TDto).Name);
                return null;
            }

            double numericValue = 0;
            if (result is Java.Lang.Number number)
            {
                numericValue = number.DoubleValue();
            }
            else
            {
                // Aggregate Energy objects from JNI reflection have getValue() returning kcal,
                // unlike individual record Energy objects where getValue() returns calories.
                // Try extracting kcal directly first, fall back to ExtractEnergyValue for other types.
                if (unit == Units.Kilocalorie
                    && (result.TryCallMethod("getInKilocalories", out double kcalValue)
                        || result.TryGetPropertyValue("value", out kcalValue)))
                {
                    numericValue = kcalValue;
                }
                else
                {
                    numericValue = result.ExtractEnergyValue();
                }
            }

            _logger.LogInformation("Aggregated {DtoName}: {Value}", typeof(TDto).Name, numericValue);

            return new AggregatedResult
            {
                StartTime = timeRange.StartTime,
                EndTime = timeRange.EndTime,
                Value = numericValue,
                Unit = unit,
                DataType = healthDataType,
                DataSdk = HealthDataSdk.GoogleHealthConnect,
                DataOrigins = dataOrigins
            };
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
        try
        {
            if (!_sdkStatus.IsSuccess)
            {
                return [];
            }

            var permission = MetricDtoExtensions.GetRequiredPermission<TDto>();
            var requestPermissionResult = await RequestPermissions([permission], false, cancellationToken);
            if (requestPermissionResult.IsError)
            {
                return [];
            }

            var healthDataType = MetricDtoExtensions.GetHealthDataType<TDto>();
            var (recordClassName, metricFieldName, unit) = GetAggregateMetricInfo(healthDataType);
            if (recordClassName is null || metricFieldName is null)
            {
                _logger.LogWarning("Interval aggregation not supported for {DtoName}", typeof(TDto).Name);
                return [];
            }

            var results = await _healthConnectClient.AggregateHealthRecordsByDuration(
                recordClassName, metricFieldName, timeRange, interval, healthDataType, unit);

            _logger.LogInformation("Found {Count} interval buckets for {DtoName}", results.Count, typeof(TDto).Name);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aggregating health data by interval for {DtoName}", typeof(TDto).Name);
            return [];
        }
    }

    public async partial Task<string?> GetChangesToken(IList<HealthDataType> dataTypes, CancellationToken cancellationToken)
    {
        try
        {
            if (!_sdkStatus.IsSuccess)
            {
                return null;
            }

            // Request read permissions for all requested data types
            var permissions = dataTypes
                .Select(dt => new HealthPermissionDto { HealthDataType = dt, PermissionType = PermissionType.Read })
                .ToList();

            var requestPermissionResult = await RequestPermissions(permissions, false, cancellationToken);
            if (requestPermissionResult.IsError)
            {
                return null;
            }

            var recordTypes = dataTypes
                .Select(dt => dt.ToKotlinClass())
                .ToList();

            var token = await _healthConnectClient.GetHealthChangesToken(recordTypes);

            _logger.LogInformation("Got changes token for {Count} data types", dataTypes.Count);
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting changes token");
            return null;
        }
    }

    public async partial Task<HealthChangesResult?> GetChanges(string token, CancellationToken cancellationToken)
    {
        try
        {
            if (!_sdkStatus.IsSuccess)
            {
                return null;
            }

            var result = await _healthConnectClient.GetHealthChanges(token);

            if (result is not null)
            {
                _logger.LogInformation("Got {Count} changes, hasMore: {HasMore}", result.Changes.Count, result.HasMore);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting health changes");
            return null;
        }
    }

    /// <summary>
    /// Maps a HealthDataType to its Android aggregate metric info (record class name, metric field name, unit).
    /// </summary>
    private static (string? RecordClassName, string? MetricFieldName, string? Unit) GetAggregateMetricInfo(HealthDataType healthDataType)
    {
        return healthDataType switch
        {
            HealthDataType.Steps => (Reflection.StepsRecordClassName, Reflection.CountTotalMetricName, null),
            HealthDataType.ActiveCaloriesBurned => (Reflection.ActiveCaloriesBurnedRecordClassName, Reflection.ActiveCaloriesTotalMetricName, Units.Kilocalorie),
            HealthDataType.Hydration => (Reflection.HydrationRecordClassName, Reflection.VolumeTotalMetricName, Units.Liter),
            HealthDataType.Weight => (Reflection.WeightRecordClassName, Reflection.WeightAvgMetricName, Units.Kilogram),
            HealthDataType.HeartRate => (Reflection.HeartRateRecordClassName, Reflection.BpmAvgMetricName, Units.BeatsPerMinute),
            _ => (null, null, null)
        };
    }

    public partial void OpenStorePageOfHealthProvider()
    {
        _activityContext.OpenHealthConnectInPlayStore();
    }

    public async partial Task<DateTime> GetEarliestAccessibleDateTime(CancellationToken cancellationToken)
    {
        // Safe conservative fallback when we have no stored anchor: now - 30 days. Health Connect
        // guarantees at least that much is accessible ("30 days prior to when any permission was
        // first granted" — a fresh grant anchors at most at "now"), so this is always within the
        // readable window.
        // Spec: https://developer.android.com/health-and-fitness/guides/health-connect/develop/read-data#read-data-older-than-30-days
        var safeFallback = DateTime.UtcNow.AddDays(-HealthConnectDefaultHistoryDays);

        try
        {
            if (!_sdkStatus.IsSuccess)
            {
                return safeFallback;
            }

            var grantedPermissions = await KotlinResolver.ProcessList<Java.Lang.String>(_healthConnectClient.PermissionController.GetGrantedPermissions);
            var hasHistoryPermission = grantedPermissions?
                .ToList()
                .Any(p => p?.ToString() == FullHistoryReadPermission) ?? false;

            if (hasHistoryPermission)
            {
                // READ_HEALTH_DATA_HISTORY lifts the 30-day cap entirely.
                // https://developer.android.com/reference/kotlin/androidx/health/connect/client/permission/HealthPermission#PERMISSION_READ_HEALTH_DATA_HISTORY()
                return DateTime.MinValue;
            }

            // Without the history permission: earliest accessible = firstGrantDate - 30 days.
            // The platform exposes no API to retrieve firstGrantDate (it lives in the system
            // permission service and is not readable by third-party apps), so we use the value
            // persisted by RequestPermissions when the first-grant transition was observed.
            // Spec: https://developer.android.com/health-and-fitness/guides/health-connect/develop/read-data#read-data-older-than-30-days
            var firstGrantMs = Preferences.Default.Get<long>(AndroidConstant.FirstPermissionGrantAtKey, 0L);
            if (firstGrantMs == 0L)
            {
                // No anchor persisted — pre-tracking install, or permissions were never granted
                // through this library version. Safe fallback keeps us inside the 30-day window.
                return safeFallback;
            }

            var firstGrantUtc = DateTimeOffset.FromUnixTimeMilliseconds(firstGrantMs).UtcDateTime;
            return firstGrantUtc.AddDays(-HealthConnectDefaultHistoryDays);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining earliest accessible DateTime; using safe fallback of UtcNow - {Days} days", HealthConnectDefaultHistoryDays);
            return safeFallback;
        }
    }
}
