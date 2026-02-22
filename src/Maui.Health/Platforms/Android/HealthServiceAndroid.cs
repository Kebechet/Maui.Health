using Android.Content;
using Android.Health.Connect.DataTypes;
using AndroidX.Activity;
using AndroidX.Activity.Result;
using AndroidX.Health.Connect.Client;
using AndroidX.Health.Connect.Client.Records;
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
    public partial bool IsSupported => IsSdkAvailable().IsSuccess;

    private Context _activityContext => Platform.CurrentActivity ??
        throw new Exception("Current activity is null");

    private IHealthConnectClient _healthConnectClient => HealthConnectClient.GetOrCreate(_activityContext);

    public async partial Task<RequestPermissionResult> RequestPermissions(IList<HealthPermissionDto> healthPermissions, bool canRequestFullHistoryPermission, CancellationToken cancellationToken)
    {
        try
        {
            var sdkCheckResult = IsSdkAvailable();
            if (!sdkCheckResult.IsSuccess)
            {
                return new()
                {
                    Error = RequestPermissionError.IsNotSupported
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
        try
        {
            _logger.LogInformation("Android GetHealthDataAsync<{DtoName}>: StartTime: {StartTime}, EndTime: {EndTime}",
                typeof(TDto).Name, timeRange.StartTime, timeRange.EndTime);

            var sdkCheckResult = IsSdkAvailable();
            if (!sdkCheckResult.IsSuccess)
            {
                return [];
            }

            // Request permission for the specific metric
            var permission = MetricDtoExtensions.GetRequiredPermission<TDto>();
            var requestPermissionResult = await RequestPermissions([permission], false, cancellationToken);
            if (requestPermissionResult.IsError)
            {
                return [];
            }

            // Use aggregate query for cumulative types to deduplicate across sources
            if (IsCumulativeType<TDto>())
            {
                return await GetCumulativeHealthDataAsync<TDto>(timeRange, cancellationToken);
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
    public partial Task<bool> WriteHealthData<TDto>(TDto data, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        return WriteHealthDataInternal(data, cancellationToken);
    }

    private async Task<bool> WriteHealthDataInternal<TDto>(TDto data, CancellationToken cancellationToken) where TDto : HealthMetricBase
    {
        try
        {
            var sdkCheckResult = IsSdkAvailable();
            if (!sdkCheckResult.IsSuccess)
            {
                return false;
            }

            // Request write permission for the specific metric
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

            var record = data.ToAndroidRecord();
            if (record is null)
            {
                _logger.LogWarning("Failed to convert {DtoName} to Android record", typeof(TDto).Name);
                return false;
            }

            var success = await _healthConnectClient.InsertRecord(record);
            if (!success)
            {
                _logger.LogWarning("Failed to insert {DtoName} record via reflection", typeof(TDto).Name);
                return false;
            }

            _logger.LogInformation("Successfully wrote {DtoName} record", typeof(TDto).Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing health data for {DtoName}", typeof(TDto).Name);
            return false;
        }
    }

    private static bool IsCumulativeType<TDto>() where TDto : HealthMetricBase
    {
        return typeof(TDto) == typeof(StepsDto) ||
               typeof(TDto) == typeof(ActiveCaloriesBurnedDto);
    }

    private async Task<List<TDto>> GetCumulativeHealthDataAsync<TDto>(HealthTimeRange timeRange, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        var (recordClassName, metricFieldName) = typeof(TDto).Name switch
        {
            nameof(StepsDto) => (Reflection.StepsRecordClassName, Reflection.CountTotalMetricName),
            nameof(ActiveCaloriesBurnedDto) => (Reflection.ActiveCaloriesBurnedRecordClassName, Reflection.ActiveCaloriesTotalMetricName),
            _ => throw new NotSupportedException($"Cumulative type {typeof(TDto).Name} is not supported")
        };

        var result = await _healthConnectClient.AggregateHealthRecords(recordClassName, metricFieldName, timeRange);
        if (result is null)
        {
            _logger.LogInformation("No aggregate data found for {DtoName}", typeof(TDto).Name);
            return [];
        }

        TDto? dto = null;

        if (typeof(TDto) == typeof(StepsDto))
        {
            long count = 0;
            if (result is Java.Lang.Number number)
            {
                count = number.LongValue();
            }

            dto = new StepsDto
            {
                Id = Guid.NewGuid().ToString(),
                DataOrigin = Constants.DataOrigin.HealthConnectOrigin,
                Timestamp = timeRange.StartTime,
                Count = count,
                StartTime = timeRange.StartTime,
                EndTime = timeRange.EndTime
            } as TDto;
        }
        else if (typeof(TDto) == typeof(ActiveCaloriesBurnedDto))
        {
            var energy = result.ExtractEnergyValue();

            dto = new ActiveCaloriesBurnedDto
            {
                Id = Guid.NewGuid().ToString(),
                DataOrigin = Constants.DataOrigin.HealthConnectOrigin,
                Timestamp = timeRange.StartTime,
                Energy = energy,
                StartTime = timeRange.StartTime,
                EndTime = timeRange.EndTime,
                Unit = Units.Kilocalorie
            } as TDto;
        }

        if (dto is null)
        {
            return [];
        }

        _logger.LogInformation("Found cumulative {DtoName}: {Value}", typeof(TDto).Name,
            dto is StepsDto steps ? steps.Count.ToString() :
            dto is ActiveCaloriesBurnedDto cal ? cal.Energy.ToString("F0") : "N/A");

        return [dto];
    }

    private Result<SdkCheckError> IsSdkAvailable() => JavaReflectionHelper.CheckSdkAvailability(_activityContext);
}
