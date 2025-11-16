using Android.Content;
using AndroidX.Health.Connect.Client;
using AndroidX.Health.Connect.Client.Request;
using AndroidX.Health.Connect.Client.Time;
using AndroidX.Health.Connect.Client.Units;
using Kotlin.Jvm;
using Java.Time;
using AndroidX.Activity;
using AndroidX.Activity.Result;
using Java.Util;
using Maui.Health.Platforms.Android.Extensions;
using AndroidX.Health.Connect.Client.Response;
using System.Diagnostics;
using Maui.Health.Platforms.Android.Callbacks;
using Maui.Health.Models;
using Maui.Health.Enums.Errors;
using Maui.Health.Enums;
using Maui.Health.Models.Metrics;
using Maui.Health.Extensions;
using StepsRecord = AndroidX.Health.Connect.Client.Records.StepsRecord;
using WeightRecord = AndroidX.Health.Connect.Client.Records.WeightRecord;
using HeightRecord = AndroidX.Health.Connect.Client.Records.HeightRecord;
using ActiveCaloriesBurnedRecord = AndroidX.Health.Connect.Client.Records.ActiveCaloriesBurnedRecord;
using HeartRateRecord = AndroidX.Health.Connect.Client.Records.HeartRateRecord;
using ExerciseSessionRecord = AndroidX.Health.Connect.Client.Records.ExerciseSessionRecord;
using BodyFatRecord = AndroidX.Health.Connect.Client.Records.BodyFatRecord;
using Vo2MaxRecord = AndroidX.Health.Connect.Client.Records.Vo2MaxRecord;
using BloodPressureRecord = AndroidX.Health.Connect.Client.Records.BloodPressureRecord;

namespace Maui.Health.Services;

public partial class HealthService
{
    public partial bool IsSupported => IsSdkAvailable().IsSuccess;

    private Context _activityContext => Platform.CurrentActivity ??
        throw new Exception("Current activity is null");

    private IHealthConnectClient _healthConnectClient => HealthConnectClient.GetOrCreate(_activityContext);

    public async partial Task<List<TDto>> GetHealthDataAsync<TDto>(HealthTimeRange timeRange, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        try
        {
            Debug.WriteLine($"Android GetHealthDataAsync<{typeof(TDto).Name}>:");
            Debug.WriteLine($"  StartTime: {timeRange.StartTime}");
            Debug.WriteLine($"  EndTime: {timeRange.EndTime}");

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

            var healthDataType = MetricDtoExtensions.GetHealthDataType<TDto>();
            var recordClass = healthDataType.ToKotlinClass();

            var timeRangeFilter = TimeRangeFilter.Between(
                Instant.OfEpochMilli(timeRange.StartTime.ToUnixTimeMilliseconds())!,
                Instant.OfEpochMilli(timeRange.EndTime.ToUnixTimeMilliseconds())!
            );

            var request = new ReadRecordsRequest(
                recordClass,
                timeRangeFilter,
                [],
                true,
                1000,
                null
            );

            var response = await KotlinResolver.Process<ReadRecordsResponse, ReadRecordsRequest>(_healthConnectClient.ReadRecords, request);
            if (response is null)
            {
                return [];
            }

            var results = new List<TDto>();

            // Special handling for WorkoutDto to add heart rate data
            if (typeof(TDto) == typeof(WorkoutDto))
            {
                for (int i = 0; i < response.Records.Count; i++)
                {
                    var record = response.Records[i];
                    if (record is Java.Lang.Object javaObject && record is ExerciseSessionRecord exerciseRecord)
                    {
                        var dto = await exerciseRecord.ToWorkoutDtoAsync(QueryHeartRateRecordsAsync, cancellationToken) as TDto;
                        if (dto is not null)
                        {
                            results.Add(dto);
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < response.Records.Count; i++)
                {
                    var record = response.Records[i];
                    if (record is Java.Lang.Object javaObject)
                    {
                        var dto = javaObject.ConvertToDto<TDto>();
                        if (dto is not null)
                        {
                            results.Add(dto);
                        }
                    }
                }
            }

            Debug.WriteLine($"  Found {results.Count} {typeof(TDto).Name} records");
            return results;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error fetching health data: {ex}");
            return [];
        }
    }

    private async Task<HeartRateDto[]> QueryHeartRateRecordsAsync(HealthTimeRange timeRange, CancellationToken cancellationToken)
    {
        try
        {
            var timeRangeFilter = TimeRangeFilter.Between(
                Instant.OfEpochMilli(timeRange.StartTime.ToUnixTimeMilliseconds())!,
                Instant.OfEpochMilli(timeRange.EndTime.ToUnixTimeMilliseconds())!
            );

            var recordClass = JvmClassMappingKt.GetKotlinClass(Java.Lang.Class.FromType(typeof(HeartRateRecord)));

            var request = new ReadRecordsRequest(
                recordClass,
                timeRangeFilter,
                [],
                true,
                1000,
                null
            );

            var response = await KotlinResolver.Process<ReadRecordsResponse, ReadRecordsRequest>(_healthConnectClient.ReadRecords, request);
            if (response is null)
            {
                return [];
            }

            var results = new List<HeartRateDto>();
            for (int i = 0; i < response.Records.Count; i++)
            {
                var record = response.Records[i];
                if (record is Java.Lang.Object javaObject)
                {
                    var dto = javaObject.ToHeartRateDto();
                    if (dto != null)
                    {
                        results.Add(dto);
                    }
                }
            }

            return results.ToArray();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error querying heart rate records: {ex}");
            return [];
        }
    }

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
                permissionsToGrant.Add("android.permission.health.READ_HEALTH_DATA_HISTORY");
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
        catch (Exception e)
        {
            return new()
            {
                ErrorException = e
            };
        }
    }

    private Result<SdkCheckError> IsSdkAvailable()
    {
        try
        {
            var availabilityStatus = HealthConnectClient.GetSdkStatus(_activityContext);
            if (availabilityStatus == HealthConnectClient.SdkUnavailable)
            {
                return new()
                {
                    Error = SdkCheckError.SdkUnavailable
                };
            }

            if (availabilityStatus == HealthConnectClient.SdkUnavailableProviderUpdateRequired)
            {
                string providerPackageName = "com.google.android.apps.healthdata";
                // Optionally redirect to package installer to find a provider, for example:
                var uriString = $"market://details?id={providerPackageName}&url=healthconnect%3A%2F%2Fonboarding";

                var intent = new Intent(Intent.ActionView);
                intent.SetPackage("com.android.vending");
                intent.SetData(Android.Net.Uri.Parse(uriString));
                intent.PutExtra("overlay", true);
                intent.PutExtra("callerId", _activityContext.PackageName);

                _activityContext.StartActivity(intent);

                return new()
                {
                    Error = SdkCheckError.SdkUnavailableProviderUpdateRequired
                };
            }

            //The Health Connect SDK supports Android 8(API level 26) or higher, while the Health Connect app is only compatible with Android 9(API level 28) or higher.
            //This means that third-party apps can support users with Android 8, but only users with Android 9 or higher can use Health Connect.
            //https://developer.android.com/health-and-fitness/guides/health-connect/develop/get-started#:~:text=the%20latest%20version.-,Note,-%3A%20The%20Health
            if (!OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                return new()
                {
                    Error = SdkCheckError.AndroidVersionNotSupported
                };
            }

            return new();
        }
        catch (Exception e)
        {
            return new()
            {
                ErrorException = e
            };
        }
    }
}
