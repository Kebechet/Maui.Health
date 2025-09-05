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

namespace Maui.Health.Services;

public partial class HealthService
{
    public partial bool IsSupported => IsSdkAvailable().IsSuccess;

    private Context _activityContext => Platform.CurrentActivity ??
        throw new Exception("Current activity is null");

    private IHealthConnectClient _healthConnectClient => HealthConnectClient.GetOrCreate(_activityContext);

    public async partial Task<TDto[]> GetHealthDataAsync<TDto>(DateTime from, DateTime to, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        try
        {
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
                Instant.OfEpochMilli(((DateTimeOffset)from).ToUnixTimeMilliseconds())!,
                Instant.OfEpochMilli(((DateTimeOffset)to).ToUnixTimeMilliseconds())!
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

            for (int i = 0; i < response.Records.Count; i++)
            {
                var record = response.Records[i];
                if (record is Java.Lang.Object javaObject)
                {
                    var dto = ConvertToDto<TDto>(javaObject);
                    if (dto is not null)
                    {
                        results.Add(dto);
                    }
                }
            }

            return results.ToArray();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error fetching health data: {ex}");
            return [];
        }
    }

    private TDto? ConvertToDto<TDto>(Java.Lang.Object record) where TDto : HealthMetricBase
    {
        return typeof(TDto).Name switch
        {
            nameof(StepsDto) => ConvertStepsRecord(record) as TDto,
            nameof(WeightDto) => ConvertWeightRecord(record) as TDto,
            nameof(HeightDto) => ConvertHeightRecord(record) as TDto,
            _ => null
        };
    }

    private StepsDto? ConvertStepsRecord(Java.Lang.Object record)
    {
        if (record is not StepsRecord stepsRecord) 
            return null;

        var startTime = DateTimeOffset.FromUnixTimeMilliseconds(stepsRecord.StartTime.ToEpochMilli());
        var endTime = DateTimeOffset.FromUnixTimeMilliseconds(stepsRecord.EndTime.ToEpochMilli());

        return new StepsDto
        {
            Id = stepsRecord.Metadata.Id,
            DataOrigin = stepsRecord.Metadata.DataOrigin.PackageName,
            Timestamp = startTime, // Use start time as the representative timestamp
            Count = stepsRecord.Count,
            StartTime = startTime,
            EndTime = endTime
        };
    }

    private WeightDto? ConvertWeightRecord(Java.Lang.Object record)
    {
        if (record is not WeightRecord weightRecord) 
            return null;

        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(weightRecord.Time.ToEpochMilli());

        // TODO: Fix Android Health Connect Mass property access
        // The Weight property is of type Mass, but we need to find the correct way to extract the value
        var weightValue = TryExtractMassValue(weightRecord.Weight);

        return new WeightDto
        {
            Id = weightRecord.Metadata.Id,
            DataOrigin = weightRecord.Metadata.DataOrigin.PackageName,
            Timestamp = timestamp,
            Value = weightValue,
            Unit = "kg"
        };
    }

    private HeightDto? ConvertHeightRecord(Java.Lang.Object record)
    {
        if (record is not HeightRecord heightRecord) 
            return null;

        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(heightRecord.Time.ToEpochMilli());

        // TODO: Fix Android Health Connect Length property access
        // The Height property is of type Length, but we need to find the correct way to extract the value
        var heightValue = TryExtractLengthValue(heightRecord.Height);

        return new HeightDto
        {
            Id = heightRecord.Metadata.Id,
            DataOrigin = heightRecord.Metadata.DataOrigin.PackageName,
            Timestamp = timestamp,
            Value = heightValue,
            Unit = "cm"
        };
    }

    private double TryExtractMassValue(Java.Lang.Object mass)
    {
        try
        {
            // Try reflection to find the actual value property
            var massClass = mass.Class;
            var valueField = massClass.GetDeclaredFields()?.FirstOrDefault(f => 
                f.Name.Contains("value", StringComparison.OrdinalIgnoreCase) ||
                f.Name.Contains("kilogram", StringComparison.OrdinalIgnoreCase));
            
            if (valueField != null)
            {
                valueField.Accessible = true;
                var value = valueField.Get(mass);
                if (value is Java.Lang.Double javaDouble)
                {
                    return javaDouble.DoubleValue();
                }
                if (value is Java.Lang.Float javaFloat)
                {
                    return javaFloat.DoubleValue();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error extracting mass value: {ex}");
        }
        
        return 70.0; // Default fallback value
    }

    private double TryExtractLengthValue(Java.Lang.Object length)
    {
        try
        {
            // Try reflection to find the actual value property
            var lengthClass = length.Class;
            var valueField = lengthClass.GetDeclaredFields()?.FirstOrDefault(f => 
                f.Name.Contains("value", StringComparison.OrdinalIgnoreCase) ||
                f.Name.Contains("meter", StringComparison.OrdinalIgnoreCase));
            
            if (valueField != null)
            {
                valueField.Accessible = true;
                var value = valueField.Get(length);
                if (value is Java.Lang.Double javaDouble)
                {
                    return javaDouble.DoubleValue() * 100; // Convert meters to cm
                }
                if (value is Java.Lang.Float javaFloat)
                {
                    return javaFloat.DoubleValue() * 100; // Convert meters to cm
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error extracting length value: {ex}");
        }
        
        return 175.0; // Default fallback value in cm
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
    //            //lastModifiedTime
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
//}//}