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
using System.Diagnostics;
using Maui.Health.Platforms.Android.Callbacks;
using Maui.Health.Models;
using Maui.Health.Enums.Errors;
using Maui.Health.Enums;
using StepsRecord = AndroidX.Health.Connect.Client.Records.StepsRecord;

namespace Maui.Health.Services;

public partial class HealthService
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
            try
            {
                var observer = ((ComponentActivity)_activityContext).ActivityResultRegistry;
                launcher = observer.Register(key, requestPermissionActivityContract, callback);
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

    //TODO:
    //Aggregate
    //AggregateGroupByDuration
    //AggregateGroupByPeriod
    //DeleteRecords(two overloads)
    //GetChanges
    //GetChangesToken
    //InsertRecords
    //ReadRecord
    //ReadRecords
    //UpdateRecords

    public async partial Task<long?> GetStepsTodayAsync(CancellationToken cancellationToken)
    {
        var permissionsToGrant = new List<HealthPermissionDto>
        {
            new()
            {
                HealthDataType = HealthDataType.Steps,
                PermissionType = PermissionType.Read
            },
        };

        var requestPermissionResult = await RequestPermissions(permissionsToGrant, false, cancellationToken);
        if (requestPermissionResult.IsError)
        {
            return null;
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
                1000, // default
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

//public async partial Task<ReadRecordResult> ReadRecords(HealthDataType healthDataType, DateTime from, DateTime until, CancellationToken cancellationToken)
//{
//    var permissionToGrant = new HealthPermissionDto
//    {
//        HealthDataType = healthDataType,
//        PermissionType = PermissionType.Read
//    };

//    var requestPermissionResult = await RequestPermissions([permissionToGrant], false, cancellationToken);
//    if (requestPermissionResult.IsError)
//    {
//        return new()
//        {
//            Error = ReadRecordError.PermissionProblem
//        };
//    }

//    var timeRangeFilter = TimeRangeFilter.Between(
//        Instant.OfEpochMilli(((DateTimeOffset)from).ToUnixTimeMilliseconds())!,
//        Instant.OfEpochMilli(((DateTimeOffset)until).ToUnixTimeMilliseconds())!
//    );

//    var request = new ReadRecordsRequest(
//        healthDataType.ToKotlinClass(),
//        timeRangeFilter,
//        [],
//        true,
//        1000, // default
//        null
//    );

//    var response = await KotlinResolver.Process<ReadRecordsResponse, ReadRecordsRequest>(_healthConnectClient.ReadRecords, request);
//    if (response is null)
//    {
//        return new()
//        {
//            Error = ReadRecordError.ProblemDuringReading
//        };
//    }

//    //var res = new List<StepsRecord>();

//    //for (int i = 0; i < response.Records.Count; i++)
//    //{
//    //    if (response.Records[i] is StepsRecord item)
//    //    {
//    //        var healthRecord = new HealthRecord
//    //        {
//    //            Id = item.Metadata.Id,
//    //            DataOrigin = item.Metadata.DataOrigin.PackageName,
//    //            //lastModifiedTime
//    //            //recordingMethod
//    //        };


//    //        item.Metadata.

//    //        res.Add(item);
//    //        Debug.WriteLine($"{item.StartTime} - {item.EndTime}, {item.Count}: {item.Metadata.DataOrigin.PackageName}");
//    //    }
//    //}

//    //var groupedByOrigin = res.GroupBy(x => x.Metadata.DataOrigin.PackageName)
//    //    .OrderBy(x => x.Key.Contains("google"))
//    //    .ThenBy(x => x.Key.Contains("samsung"));

//    //return groupedByOrigin
//    //    .FirstOrDefault()?
//    //    .Sum(x => x.Count)
//    //    ?? 0;

//    return new();
//}