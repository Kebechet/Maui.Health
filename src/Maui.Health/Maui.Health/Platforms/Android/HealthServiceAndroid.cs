using Android.Content;
using AndroidX.Health.Connect.Client;
using AndroidX.Health.Connect.Client.Request;
using AndroidX.Health.Connect.Client.Time;
using Android.Gms.Common.Apis;
using Kotlin.Jvm;
using Java.Time;
using AndroidX.Health.Connect.Client.Permission;

using StepsRecord = AndroidX.Health.Connect.Client.Records.StepsRecord;
using AndroidX.Activity;
using AndroidX.Activity.Result;
using Java.Util;
using Maui.Health.Platforms.Android.Extensions;
using AndroidX.Health.Connect.Client.Response;
using System.Diagnostics;
using Maui.Health.Platforms.Android.Callbacks;

namespace Maui.Health.Services;

public partial class HealthService
{
    // You can update this based on a proper Health Connect availability check.
    public partial bool IsSupported => true;

    private Context _activityContext => Platform.CurrentActivity ??
        throw new System.Exception("Current activity is null");

    private IHealthConnectClient _healthConnectClient => HealthConnectClient.GetOrCreate(_activityContext);

    //bool IsGooglePlayServicesAvailable()
    //{
    //    var googleApi = GoogleApiAvailability.Instance;
    //    var status = googleApi.IsGooglePlayServicesAvailable(this.platform.CurrentActivity);

    //    return status == ConnectionResult.Success;
    //}

    public async partial Task<long?> GetStepsTodayAsync(CancellationToken cancellationToken)
    {
        var availabilityStatus = HealthConnectClient.GetSdkStatus(_activityContext);
        if (availabilityStatus == HealthConnectClient.SdkUnavailable)
        {
            return null;
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
            return null;
        }

        //The Health Connect SDK supports Android 8(API level 26) or higher, while the Health Connect app is only compatible with Android 9(API level 28) or higher.
        //This means that third-party apps can support users with Android 8, but only users with Android 9 or higher can use Health Connect.
        //https://developer.android.com/health-and-fitness/guides/health-connect/develop/get-started#:~:text=the%20latest%20version.-,Note,-%3A%20The%20Health
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            return null;
        }

        List<string> permissionsToGrant =
        [
            HealthPermission.GetReadPermission(JvmClassMappingKt.GetKotlinClass(Java.Lang.Class.FromType(typeof(StepsRecord)))),
            HealthPermission.GetWritePermission(JvmClassMappingKt.GetKotlinClass(Java.Lang.Class.FromType(typeof(StepsRecord)))),
        ];

        var grantedPermissions = await KotlinResolver.ProcessList<Java.Lang.String>(_healthConnectClient.PermissionController.GetGrantedPermissions);
        if(grantedPermissions is null)
        {
            return null;
        }

        var missingPermissions = new List<string>();

        if (missingPermissions.Any())
        {
            var key = Guid.NewGuid().ToString();

            var requestPermissionActivityContract = PermissionController.CreateRequestPermissionResultContract();

            var callback = new AndroidActivityResultCallback<ISet?>(cancellationToken);

            ActivityResultLauncher? launcher = null;
            ISet? newlyGrantedPermissions = null;
            try
            {
                var observer = ((ComponentActivity)_activityContext).ActivityResultRegistry;
                launcher = observer.Register(key, requestPermissionActivityContract, callback);
                launcher.Launch(new Java.Util.HashSet(missingPermissions));

                newlyGrantedPermissions = await callback.Task;
            }
            finally
            {
                launcher?.Unregister();
            }

            var areAllPermissionsGranted = missingPermissions.All(permission => newlyGrantedPermissions?.ToList().Contains(permission) ?? false);
            if (!areAllPermissionsGranted)
            {
                return null;
            }
        }

        try
        {
            var now = DateTimeOffset.UtcNow;
            var startOfDay = new DateTimeOffset(now.Date, TimeSpan.Zero);

            var stepCountRecordClass = JvmClassMappingKt.GetKotlinClass(Java.Lang.Class.FromType(typeof(StepsRecord)));

            var timeRangeFilter = TimeRangeFilter.Between(
                Instant.OfEpochMilli(startOfDay.ToUnixTimeMilliseconds())!,
                Instant.OfEpochMilli(now.ToUnixTimeMilliseconds())!
            );

            var request = new ReadRecordsRequest(
                stepCountRecordClass,
                timeRangeFilter,
                [],
                true,
                100,
                null
            );

            var response = await KotlinResolver.Process<ReadRecordsResponse, ReadRecordsRequest>(_healthConnectClient.ReadRecords, request);
            if(response is null)
            {
                return null;
            }

            var res = new List<StepsRecord>();

            for (int i = 0; i < response.Records.Count; i++)
            {
                if (response.Records[i] is StepsRecord item)
                {
                    res.Add(item);
                    Debug.WriteLine($"{item.StartTime} - {item.EndTime}, {item.Count}: {item.Metadata.DataOrigin.PackageName}");
                }
            }

            var groupedByOrigin = res.GroupBy(x => x.Metadata.DataOrigin.PackageName)
                .OrderBy(x => x.Key.Contains("google"))
                .ThenBy(x => x.Key.Contains("samsung"));

            return groupedByOrigin
                .FirstOrDefault()?
                .Sum(x => x.Count)
                ?? 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error fetching steps: {ex}");
            return 0;
        }
    }
}
