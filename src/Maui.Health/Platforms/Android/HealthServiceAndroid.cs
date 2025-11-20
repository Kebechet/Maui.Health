using Android.Content;
using AndroidX.Health.Connect.Client;
using AndroidX.Health.Connect.Client.Request;
using AndroidX.Health.Connect.Client.Time;
using Kotlin.Jvm;
using Java.Time;
using AndroidX.Activity;
using AndroidX.Activity.Result;
using Java.Util;
using Maui.Health.Platforms.Android.Extensions;
using AndroidX.Health.Connect.Client.Response;
using Maui.Health.Platforms.Android.Callbacks;
using Maui.Health.Models;
using Maui.Health.Enums.Errors;
using Maui.Health.Models.Metrics;
using Maui.Health.Extensions;
using Microsoft.Extensions.Logging;
using HeartRateRecord = AndroidX.Health.Connect.Client.Records.HeartRateRecord;
using ExerciseSessionRecord = AndroidX.Health.Connect.Client.Records.ExerciseSessionRecord;
using Maui.Health.Enums;
using static Maui.Health.Constants.HealthConstants;

namespace Maui.Health.Services;

public partial class HealthService
{
    private const int _minimalApiVersionRequired = Android.MinimumApiVersion; // Android 8.0

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
                permissionsToGrant.Add(Android.FullHistoryReadPermission);
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

    public async partial Task<List<TDto>> GetHealthData<TDto>(HealthTimeRange timeRange, CancellationToken cancellationToken)
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

            var healthDataType = MetricDtoExtensions.GetHealthDataType<TDto>();
            var recordClass = healthDataType.ToKotlinClass();

#pragma warning disable CA1416
            var timeRangeFilter = TimeRangeFilter.Between(
                Instant.OfEpochMilli(timeRange.StartTime.ToUnixTimeMilliseconds())!,
                Instant.OfEpochMilli(timeRange.EndTime.ToUnixTimeMilliseconds())!
            );
#pragma warning restore CA1416

            var request = new ReadRecordsRequest(
                recordClass,
                timeRangeFilter,
                [],
                true,
                Defaults.MaxRecordsPerRequest,
                null
            );

            var response = await KotlinResolver.Process<ReadRecordsResponse, ReadRecordsRequest>(_healthConnectClient.ReadRecords, request);
            if (response is null)
            {
                return [];
            }

            var results = new List<TDto>();
            for (int i = 0; i < response.Records.Count; i++)
            {
                var record = response.Records[i];
                if (record is not Java.Lang.Object javaObject)
                    continue;

                var dto = javaObject.ConvertToDto<TDto>();
                if (dto is not null)
                    results.Add(dto);
            }

            _logger.LogInformation("Found {Count} {DtoName} records", results.Count, typeof(TDto).Name);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching health data for {DtoName}", typeof(TDto).Name);
            return new List<TDto>();
        }
    }

    public async partial Task<bool> WriteHealthData<TDto>(TDto data, CancellationToken cancellationToken) where TDto : HealthMetricBase
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
            if (record == null)
            {
                _logger.LogWarning("Failed to convert {DtoName} to Android record", typeof(TDto).Name);
                return false;
            }

            // Create a Java ArrayList with the record
            var recordsList = new Java.Util.ArrayList();
            recordsList.Add(record);

            // Call InsertRecords - it's a suspend function
            // Use reflection to get the Java class from the interface implementation
            var clientType = _healthConnectClient.GetType();
            var handleField = clientType.GetField(Android.JniHandleFieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (handleField != null)
            {
                var handle = handleField.GetValue(_healthConnectClient);
                if (handle is IntPtr jniHandle && jniHandle != IntPtr.Zero)
                {
                    // Get the Java class
                    var classHandle = Android.Runtime.JNIEnv.GetObjectClass(jniHandle);
                    var clientClass = Java.Lang.Object.GetObject<Java.Lang.Class>(classHandle, Android.Runtime.JniHandleOwnership.DoNotTransfer);

                    // Get the client as a Java.Lang.Object for method invocation
                    var clientObject = Java.Lang.Object.GetObject<Java.Lang.Object>(jniHandle, Android.Runtime.JniHandleOwnership.DoNotTransfer);

                    var insertMethod = clientClass?.GetDeclaredMethod("insertRecords",
                        Java.Lang.Class.FromType(typeof(Java.Util.IList)),
                        Java.Lang.Class.FromType(typeof(Kotlin.Coroutines.IContinuation)));

                    if (insertMethod != null && clientObject != null)
                    {
                        var taskCompletionSource = new TaskCompletionSource<Java.Lang.Object>();
                        var continuation = new Continuation(taskCompletionSource, default);

                        insertMethod.Accessible = true;
                        var result = insertMethod.Invoke(clientObject, recordsList, continuation);

                        if (result is Java.Lang.Enum javaEnum)
                        {
                            var currentState = Enum.Parse<CoroutineState>(javaEnum.ToString());
                            if (currentState == CoroutineState.COROUTINE_SUSPENDED)
                            {
                                await taskCompletionSource.Task;
                            }
                        }

                        _logger.LogInformation("Successfully wrote {DtoName} record", typeof(TDto).Name);
                        return true;
                    }
                }
            }

            _logger.LogWarning("Could not find InsertRecords method via reflection");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing health data for {DtoName}", typeof(TDto).Name);
            return false;
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
                string providerPackageName = Android.HealthConnectPackage;
                // Optionally redirect to package installer to find a provider, for example:
                var uriString = string.Format(Android.PlayStoreUriTemplate, providerPackageName);

                var intent = new Intent(Intent.ActionView);
                intent.SetPackage(Android.PlayStorePackage);
                intent.SetData(global::Android.Net.Uri.Parse(uriString));
                intent.PutExtra(Android.IntentExtraOverlay, true);
                intent.PutExtra(Android.IntentExtraCaller, _activityContext.PackageName);

                _activityContext.StartActivity(intent);

                return new()
                {
                    Error = SdkCheckError.SdkUnavailableProviderUpdateRequired
                };
            }

            //The Health Connect SDK supports Android 8(API level 26) or higher, while the Health Connect app is only compatible with Android 9(API level 28) or higher.
            //This means that third-party apps can support users with Android 8, but only users with Android 9 or higher can use Health Connect.
            //https://developer.android.com/health-and-fitness/guides/health-connect/develop/get-started#:~:text=the%20latest%20version.-,Note,-%3A%20The%20Health
            if (!OperatingSystem.IsAndroidVersionAtLeast(_minimalApiVersionRequired))
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
