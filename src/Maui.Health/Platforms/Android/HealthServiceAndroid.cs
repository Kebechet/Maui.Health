using Android.Content;
using AndroidX.Health.Connect.Client;
using AndroidX.Health.Connect.Client.Request;
using AndroidX.Health.Connect.Client.Time;
using Kotlin.Jvm;
using Java.Time;
using AndroidX.Activity;
using AndroidX.Activity.Result;
using AndroidX.Health.Connect.Client.Changes;
using AndroidX.Health.Connect.Client.Records.Metadata;
using Java.Util;
using Maui.Health.Platforms.Android.Extensions;
using AndroidX.Health.Connect.Client.Response;
using Kotlin.Reflect;
using Maui.Health.Platforms.Android.Callbacks;
using Maui.Health.Models;
using Maui.Health.Enums.Errors;
using Maui.Health.Models.Metrics;
using Maui.Health.Extensions;
using Maui.Health.Models.Requests;
using Maui.Health.Models.Responses;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.PlatformConfiguration;
using HeartRateRecord = AndroidX.Health.Connect.Client.Records.HeartRateRecord;
using ExerciseSessionRecord = AndroidX.Health.Connect.Client.Records.ExerciseSessionRecord;
using IList = System.Collections.IList;

namespace Maui.Health.Services;

public partial class HealthService
{
    private DateTimeOffset? _rateLimitLastDateTimeOffsetUtc;
    private const int _minimalApiVersionRequired = 26; // Android 8.0

    public partial bool IsSupported => IsSdkAvailable().IsSuccess;

    private Context _activityContext => Platform.CurrentActivity ??
        throw new Exception("Current activity is null");

    private IHealthConnectClient _healthConnectClient => HealthConnectClient.GetOrCreate(_activityContext);

    public async partial Task<RequestPermissionResult> CheckPermissionStatusAsync(IList<HealthPermissionDto> healthPermissions, bool canRequestReadInBackgroundPermission, bool canRequestFullHistoryPermission, CancellationToken cancellationToken)
    {
        try
        {
            var sdkCheckResult = IsSdkAvailable(false);
            if (sdkCheckResult.IsError)
            {
                if (sdkCheckResult.Error == SdkCheckError.AndroidVersionNotSupported)
                {
                    return new() { Error = RequestPermissionError.IsNotSupported };
                }

                if (sdkCheckResult.Error == SdkCheckError.SdkUnavailableProviderUpdateRequired)
                {
                    return new() { Error = RequestPermissionError.AndroidSdkUnavailableProviderUpdateRequired };
                }
            }

            var permissionsToGrant = healthPermissions
                .SelectMany(healthPermission => healthPermission.ToStrings())
                .ToList();

            if (canRequestFullHistoryPermission)
            {
                //https://developer.android.com/health-and-fitness/guides/health-connect/plan/data-types#alpha10
                permissionsToGrant.Add("android.permission.health.READ_HEALTH_DATA_HISTORY");
            }

            if (canRequestReadInBackgroundPermission)
            {
                permissionsToGrant.Add("android.permission.health.READ_HEALTH_DATA_IN_BACKGROUND");
            }

            var grantedPermissions =
                await KotlinResolver.ProcessList<Java.Lang.String>(_healthConnectClient.PermissionController
                    .GetGrantedPermissions);
            if (grantedPermissions is null)
            {
                return new() { Error = RequestPermissionError.ProblemWhileFetchingAlreadyGrantedPermissions };
            }

            var missingPermissions = permissionsToGrant
                .Where(permission => !grantedPermissions.ToList().Contains(permission))
                .ToList();

            if (!missingPermissions.Any())
            {
                return new();
            }

            if (grantedPermissions.Count == 0)
            {
                return new() { Error = RequestPermissionError.MissingPermissions };
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

    public async partial Task<RequestPermissionResult> RequestPermissions(IList<HealthPermissionDto> healthPermissions, bool canRequestReadInBackgroundPermission, bool canRequestFullHistoryPermission, CancellationToken cancellationToken)
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

            if (canRequestReadInBackgroundPermission)
            {
                permissionsToGrant.Add("android.permission.health.READ_HEALTH_DATA_IN_BACKGROUND");
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

    public async partial Task<List<TDto>> GetHealthDataAsync<TDto>(HealthTimeRange timeRange, CancellationToken cancellationToken)
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
            //I dont think we should be asking for permissions here
            // Request permission for the specific metric
            /*var permission = MetricDtoExtensions.GetRequiredPermission<TDto>();
            var requestPermissionResult = await RequestPermissions([permission], false, cancellationToken);
            if (requestPermissionResult.IsError)
            {
                return [];
            }*/

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
                1000,
                null
            );

            var response = await KotlinResolver.Process<ReadRecordsResponse, ReadRecordsRequest>(_healthConnectClient.ReadRecords, request);
            if (response is null)
            {
                return [];
            }

            var results = await ConvertRecordsToDtos<TDto>(response.Records, cancellationToken);

            _logger.LogInformation("Found {Count} {DtoName} records", results.Count, typeof(TDto).Name);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching health data for {DtoName}", typeof(TDto).Name);
            return [];
        }
    }

    public async partial Task<ReadHealthDataResponse<TDto>> GetHealthDataAsync<TDto>(ReadHealthDataRequest request,
        CancellationToken cancellationToken) where TDto : HealthMetricBase
    {
        try
        {
            _logger.LogInformation("Android GetHealthDataAsync<{DtoName}>: StartTime: {StartTime}, EndTime: {EndTime}",
                typeof(TDto).Name, request.HealthTimeRange.StartTime, request.HealthTimeRange.EndTime);

            var sdkCheckResult = IsSdkAvailable(false);
            if (!sdkCheckResult.IsSuccess)
            {
                return new ReadHealthDataResponse<TDto>();
            }

            //I dont think we should be asking for permissions here
            // Request permission for the specific metric
            /*var permission = MetricDtoExtensions.GetRequiredPermission<TDto>();
            var requestPermissionResult = await RequestPermissions([permission], false, cancellationToken);
            if (requestPermissionResult.IsError)
            {
                return new ReadHealthDataResponse<TDto>();
            }*/

            var healthDataType = MetricDtoExtensions.GetHealthDataType<TDto>();
            var recordClass = healthDataType.ToKotlinClass();

#pragma warning disable CA1416
            var timeRangeFilter = TimeRangeFilter.Between(
                Instant.OfEpochMilli(request.HealthTimeRange.StartTime.ToUnixTimeMilliseconds())!,
                Instant.OfEpochMilli(request.HealthTimeRange.EndTime.ToUnixTimeMilliseconds())!
            );
#pragma warning restore CA1416

            var dataOriginFilter = CreateOriginFilter(request.OriginFilter);
            if (dataOriginFilter.Count > 0)
            {
                _logger.LogInformation("Using origin filter: {@OriginFilter}", request.OriginFilter);
            }

            string? pageToken = null;
            if (request.PageTokenOrAnchor != null)
            {
                pageToken = request.PageTokenOrAnchor.ToString();
                _logger.LogDebug("Using page token: {PageToken}", string.IsNullOrEmpty(pageToken) ? "<null>" : pageToken);
            }

            var readRequest = new ReadRecordsRequest(
                recordClass,
                timeRangeFilter,
                dataOriginFilter,
                true,
                request.PageSize,
                pageToken
            );

            global::System.Collections.IList allRecords = new Android.Runtime.JavaList();
            string? nextPageToken = null;

            do
            {
                var response =
                    await KotlinResolver.Process<ReadRecordsResponse, ReadRecordsRequest>(
                        _healthConnectClient.ReadRecords,
                        readRequest);
                if (response is null)
                {
                    return new ReadHealthDataResponse<TDto>();
                }
                _logger.LogTrace("response.Records.Count {token}", response.Records.Count);
                _logger.LogTrace("response.PageToken {token}", response.PageToken);
                nextPageToken = string.IsNullOrEmpty(response.PageToken) ? null : response.PageToken;

                // Add current batch to our collection
                foreach (var rec in response.Records)
                {
                    allRecords.Add(rec);
                }

                if (!request.ReadAll)
                {
                    break;
                }

                _logger.LogTrace("nextPageToken {token}", nextPageToken);
                // Prepare for the next page if we are reading all
                if (nextPageToken != null)
                {
                    readRequest = new ReadRecordsRequest(
                        recordClass,
                        timeRangeFilter,
                        dataOriginFilter,
                        true,
                        request.PageSize,
                        nextPageToken
                    );
                }
            } while (nextPageToken != null);

            var results = await ConvertRecordsToDtos<TDto>(allRecords, cancellationToken);

            _logger.LogInformation("Found {Count} {DtoName} records", results.Count, typeof(TDto).Name);
            _logger.LogDebug("Next page token: {PageToken}", string.IsNullOrEmpty(nextPageToken) ? "<null>" : nextPageToken);

            var readHealthDataResponse = new ReadHealthDataResponse<TDto>
            {
                Records = results,
                PageTokenOrAnchor = string.IsNullOrEmpty(nextPageToken) ? null : nextPageToken
            };
            _rateLimitLastDateTimeOffsetUtc = null;
            return readHealthDataResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching health data for {DtoName}", typeof(TDto).Name);
            var response = new ReadHealthDataResponse<TDto>
            {
                IsError = true,
                ErrorException = ex
            };
            if (ex.Message.Contains("Rate limit") && ex.Message.Contains("exceeded"))
            {
                _rateLimitLastDateTimeOffsetUtc = DateTimeOffset.UtcNow;
                response.IsRateExceeded = true;
                response.RateLimitLastDateTimeOffsetUtc = _rateLimitLastDateTimeOffsetUtc;
            }
            return response;
        }
    }

    public async partial Task<GetChangesResponse<TDto>> GetHealthDataChangesAsync<TDto>(GetChangesRequest request,
        CancellationToken cancellationToken) where TDto : HealthMetricBase
    {
        var healthDataType = MetricDtoExtensions.GetHealthDataType<TDto>();
        var recordClass = healthDataType.ToKotlinClass();

        if (request.ChangeTokenOrAnchor == null)
        {
            //get a token
            var dataOriginFilter = CreateOriginFilter(request.OriginFilter);
            request.ChangeTokenOrAnchor = await GetChangesToken(recordClass, dataOriginFilter);
        }

        //get changes
        var changeToken = request.ChangeTokenOrAnchor?.ToString();
        if(changeToken == null)
        {
            throw new Exception("Change token is null");
        }

        var allChanges = new List<IChange>();
        var nextChangesToken = changeToken;
        var hasMoreChanges = false;
        try
        {
            do
            {
                var changes =
                    await KotlinResolver.Process<ChangesResponse, string>(_healthConnectClient.GetChanges,
                        nextChangesToken);

                if (changes is null)
                {
                    return new GetChangesResponse<TDto> { IsError = true, ErrorMessage = "Changes response is null", };
                }

                //if expired token, get a new one
                if (changes.ChangesTokenExpired)
                {
                    //get a new token
                    nextChangesToken = await GetChangesToken(recordClass, CreateOriginFilter(request.OriginFilter));
                    hasMoreChanges = true;
                    continue;
                }

                allChanges.AddRange(changes.Changes);
                //_logger.LogTrace("Current Token {token}", nextChangesToken);
                //_logger.LogTrace("    New Token {token}", changes.NextChangesToken);
                nextChangesToken = changes.NextChangesToken;
                hasMoreChanges = changes.HasMore;

            } while (hasMoreChanges);

            var response = new GetChangesResponse<TDto>
            {
                Records = await ConvertRecordsToDtos<TDto>(allChanges, cancellationToken),
                ChangeTokenOrAnchor = nextChangesToken
            };
            return response;
        }
        catch (Exception ex)
        {
            return new GetChangesResponse<TDto>
            {
                IsError = true,
                ErrorMessage = ex.Message,
                ErrorException = ex
            };
        }
    }

    private async Task<HeartRateDto[]> QueryHeartRateRecordsAsync(HealthTimeRange timeRange, CancellationToken cancellationToken)
    {
        try
        {
#pragma warning disable CA1416
            var timeRangeFilter = TimeRangeFilter.Between(
                Instant.OfEpochMilli(timeRange.StartTime.ToUnixTimeMilliseconds())!,
                Instant.OfEpochMilli(timeRange.EndTime.ToUnixTimeMilliseconds())!
            );
#pragma warning restore CA1416

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
            _logger.LogError(ex, "Error querying heart rate records");
            return [];
        }
    }

    private Result<SdkCheckError> IsSdkAvailable(bool runUpdateInstallUi = true)
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
                if (!runUpdateInstallUi)
                {
                    return new() { Error = SdkCheckError.SdkUnavailableProviderUpdateRequired };
                }

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

    private async Task<List<TDto>> ConvertRecordsToDtos<TDto>(IList records, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        var results = new List<TDto>();
        // Special handling for WorkoutDto to add heart rate data
        foreach (var record in records)
        {
            if (record is not Java.Lang.Object javaObject)
                continue;

            TDto? dto;
            // Special WorkoutDto handling
            if (typeof(TDto) == typeof(WorkoutDto) && record is ExerciseSessionRecord exerciseRecord)
            {
                dto = await exerciseRecord.ToWorkoutDtoAsync(QueryHeartRateRecordsAsync, cancellationToken) as TDto;
            }
            else
            {
                dto = javaObject.ConvertToDto<TDto>();
            }

            if (dto is not null)
                results.Add(dto);
        }

        return results;
    }

    private async Task<string> GetChangesToken(IKClass recordClass, ICollection<DataOrigin> dataOriginFilter)
    {

            //get a token
            var changeTokenRequest = new ChangesTokenRequest([recordClass], dataOriginFilter);
            var tokenObj = await KotlinResolver.Process<Java.Lang.Object, ChangesTokenRequest>(
                _healthConnectClient.GetChangesToken,
                changeTokenRequest);

            if (tokenObj is null)
            {
                throw new Exception("Failed to get changes token");
            }
            return tokenObj.ToString();
    }

    private ICollection<DataOrigin> CreateOriginFilter(List<string>? requestOriginFilter)
    {
        var dataOriginFilter = new List<DataOrigin>();

        if (requestOriginFilter != null)
        {
            foreach (var filter in requestOriginFilter)
            {
                dataOriginFilter.Add(new DataOrigin(filter));
            }
        }

        return dataOriginFilter;
    }
}
