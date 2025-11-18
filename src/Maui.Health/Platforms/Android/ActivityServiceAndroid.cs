using Android.Content;
using AndroidX.Health.Connect.Client;
using AndroidX.Health.Connect.Client.Records;
using AndroidX.Health.Connect.Client.Request;
using AndroidX.Health.Connect.Client.Time;
using Java.Time;
using Maui.Health.Enums;
using Maui.Health.Models.Metrics;
using Maui.Health.Platforms.Android.Callbacks;
using Maui.Health.Platforms.Android.Extensions;
using Microsoft.Extensions.Logging;


namespace Maui.Health.Services;

public partial class ActivityService
{
    private ExerciseSessionRecord? _activeSession;
    private WorkoutDto? _activeWorkoutDto;
    private readonly ILogger<ActivityService>? _logger;

    private Context _activityContext => Platform.CurrentActivity ??
        throw new Exception("Current activity is null");

    private IHealthConnectClient _healthConnectClient => HealthConnectClient.GetOrCreate(_activityContext);

    public ActivityService(ILogger<ActivityService>? logger = null)
    {
        _logger = logger;
    }

    public partial async Task<List<WorkoutDto>> Read(HealthTimeRange activityTime)
    {
        try
        {
            _logger?.LogInformation("Android ActivityService Read: StartTime: {StartTime}, EndTime: {EndTime}",
                activityTime.StartTime, activityTime.EndTime);

#pragma warning disable CA1416
            var timeRangeFilter = TimeRangeFilter.Between(
                Instant.OfEpochMilli(activityTime.StartTime.ToUnixTimeMilliseconds())!,
                Instant.OfEpochMilli(activityTime.EndTime.ToUnixTimeMilliseconds())!
            );
#pragma warning restore CA1416

            var recordClass = Kotlin.Jvm.JvmClassMappingKt.GetKotlinClass(
                Java.Lang.Class.FromType(typeof(ExerciseSessionRecord)));

            var request = new ReadRecordsRequest(
                recordClass,
                timeRangeFilter,
                [],
                true,
                1000,
                null
            );

            var response = await KotlinResolver.Process<AndroidX.Health.Connect.Client.Response.ReadRecordsResponse, ReadRecordsRequest>(
                _healthConnectClient.ReadRecords, request);

            if (response is null)
            {
                return [];
            }

            var results = new List<WorkoutDto>();
            for (int i = 0; i < response.Records.Count; i++)
            {
                var record = response.Records[i];
                if (record is ExerciseSessionRecord exerciseRecord)
                {
                    var dto = exerciseRecord.ToWorkoutDto();
                    if (dto is not null)
                        results.Add(dto);
                }
            }

            _logger?.LogInformation("Android ActivityService: Found {Count} workout records", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Android ActivityService Read error");
            return [];
        }
    }

    public partial Task<WorkoutDto> ReadActive(HealthTimeRange activityTime)
    {
        try
        {
            // Return from memory if available
            if (_activeWorkoutDto is not null)
            {
                return Task.FromResult(_activeWorkoutDto);
            }

            // COMMENTED OUT: Try to reconstruct from preferences (for app restarts)
            // NOTE: Session persistence disabled
            //var activeSessionId = Preferences.Default.Get("ActiveSessionId", string.Empty);
            //if (!string.IsNullOrEmpty(activeSessionId))
            //{
            //    var activityTypeStr = Preferences.Default.Get("ActiveSessionActivityType", string.Empty);
            //    var title = Preferences.Default.Get("ActiveSessionTitle", string.Empty);
            //    var startTimeMs = Preferences.Default.Get("ActiveSessionStartTime", 0L);
            //    var dataOrigin = Preferences.Default.Get("ActiveSessionDataOrigin", string.Empty);
            //
            //    if (Enum.TryParse<ActivityType>(activityTypeStr, out var activityType) && startTimeMs > 0)
            //    {
            //        _activeWorkoutDto = new WorkoutDto
            //        {
            //            Id = activeSessionId,
            //            DataOrigin = dataOrigin,
            //            ActivityType = activityType,
            //            Title = string.IsNullOrEmpty(title) ? null : title,
            //            StartTime = DateTimeOffset.FromUnixTimeMilliseconds(startTimeMs),
            //            EndTime = null, // Active session - no end time yet
            //            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(startTimeMs)
            //        };
            //        return Task.FromResult(_activeWorkoutDto);
            //    }
            //}

            return Task.FromResult<WorkoutDto>(null!);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Android ActivityService ReadActive error");
            return Task.FromResult<WorkoutDto>(null!);
        }
    }

    public partial async Task Write(WorkoutDto workout)
    {
        try
        {
            _logger?.LogInformation("Android ActivityService Write: {ActivityType}", workout.ActivityType);

            var record = workout.ToExerciseSessionRecord();
            if (record == null)
            {
                _logger?.LogWarning("Failed to convert WorkoutDto to ExerciseSessionRecord");
                return;
            }

            var recordsList = new Java.Util.ArrayList();
            recordsList.Add(record);

            var clientType = _healthConnectClient.GetType();
            var handleField = clientType.GetField("handle",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (handleField != null)
            {
                var handle = handleField.GetValue(_healthConnectClient);
                if (handle is IntPtr jniHandle && jniHandle != IntPtr.Zero)
                {
                    var classHandle = Android.Runtime.JNIEnv.GetObjectClass(jniHandle);
                    var clientClass = Java.Lang.Object.GetObject<Java.Lang.Class>(
                        classHandle, Android.Runtime.JniHandleOwnership.DoNotTransfer);

                    var clientObject = Java.Lang.Object.GetObject<Java.Lang.Object>(
                        jniHandle, Android.Runtime.JniHandleOwnership.DoNotTransfer);

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

                        _logger?.LogInformation("Successfully wrote workout record to Health Connect");
                    }
                    else
                    {
                        _logger?.LogWarning("Failed to invoke insertRecords method");
                    }
                }
                else
                {
                    _logger?.LogWarning("Failed to get JNI handle for Health Connect client");
                }
            }
            else
            {
                _logger?.LogWarning("Failed to get handle field from Health Connect client");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Android ActivityService Write error");
            throw; // Re-throw so caller knows it failed
        }
    }

    public partial async Task Delete(WorkoutDto workout)
    {
        try
        {
            _logger?.LogInformation("Android ActivityService Delete: {WorkoutId}", workout.Id);

            // Create a list with the workout ID to delete
            var recordIdsList = new Java.Util.ArrayList();
            recordIdsList.Add(workout.Id);

            var clientType = _healthConnectClient.GetType();
            var handleField = clientType.GetField("handle",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (handleField != null)
            {
                var handle = handleField.GetValue(_healthConnectClient);
                if (handle is IntPtr jniHandle && jniHandle != IntPtr.Zero)
                {
                    var classHandle = Android.Runtime.JNIEnv.GetObjectClass(jniHandle);
                    var clientClass = Java.Lang.Object.GetObject<Java.Lang.Class>(
                        classHandle, Android.Runtime.JniHandleOwnership.DoNotTransfer);

                    var clientObject = Java.Lang.Object.GetObject<Java.Lang.Object>(
                        jniHandle, Android.Runtime.JniHandleOwnership.DoNotTransfer);

                    // Get the KClass for ExerciseSessionRecord
                    var recordClass = Kotlin.Jvm.JvmClassMappingKt.GetKotlinClass(
                        Java.Lang.Class.FromType(typeof(ExerciseSessionRecord)));

                    var deleteMethod = clientClass?.GetDeclaredMethod("deleteRecords",
                        Java.Lang.Class.FromType(typeof(Kotlin.Reflect.IKClass)),
                        Java.Lang.Class.FromType(typeof(Java.Util.IList)),
                        Java.Lang.Class.FromType(typeof(Java.Util.IList)),
                        Java.Lang.Class.FromType(typeof(Kotlin.Coroutines.IContinuation)));

                    if (deleteMethod != null && clientObject != null)
                    {
                        var taskCompletionSource = new TaskCompletionSource<Java.Lang.Object>();
                        var continuation = new Continuation(taskCompletionSource, default);

                        deleteMethod.Accessible = true;
                        var emptyList = new Java.Util.ArrayList(); // Empty client record ID list

                        // Cast recordClass to Java.Lang.Object for the Invoke call
                        var recordClassObj = Java.Lang.Object.GetObject<Java.Lang.Object>(
                            recordClass.Handle, Android.Runtime.JniHandleOwnership.DoNotTransfer);

                        var result = deleteMethod.Invoke(clientObject, recordClassObj, recordIdsList, emptyList, continuation);

                        if (result is Java.Lang.Enum javaEnum)
                        {
                            var currentState = Enum.Parse<CoroutineState>(javaEnum.ToString());
                            if (currentState == CoroutineState.COROUTINE_SUSPENDED)
                            {
                                await taskCompletionSource.Task;
                            }
                        }

                        _logger?.LogInformation("Successfully deleted workout record from Health Connect");
                    }
                    else
                    {
                        _logger?.LogWarning("Failed to invoke deleteRecords method");
                    }
                }
                else
                {
                    _logger?.LogWarning("Failed to get JNI handle for Health Connect client");
                }
            }
            else
            {
                _logger?.LogWarning("Failed to get handle field from Health Connect client");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Android ActivityService Delete error");
            throw; // Re-throw so caller knows it failed
        }
    }

    public partial Task<bool> IsSessionRunning()
    {
        try
        {
            // Check memory only (no persistence check)
            // NOTE: Session persistence disabled
            if (_activeWorkoutDto is not null)
                return Task.FromResult(true);

            // COMMENTED OUT: Check if there's a persisted active session
            //var activeSessionId = Preferences.Default.Get("ActiveSessionId", string.Empty);
            //return Task.FromResult(!string.IsNullOrEmpty(activeSessionId));

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Android ActivityService IsSessionRunning error");
            return Task.FromResult(false);
        }
    }

    public partial Task StartNewSession(WorkoutDto workoutDto)
    {
        try
        {
            _logger?.LogInformation("Android ActivityService StartNewSession: {ActivityType}", workoutDto.ActivityType);

            // Store the workout DTO for tracking in memory only
            // Don't create ExerciseSessionRecord yet - Android requires endTime > startTime
            // We'll create it when EndActiveSession is called with the actual end time
            _activeWorkoutDto = workoutDto;
            _activeSession = null; // Mark as active but not yet persisted

            // COMMENTED OUT: Persist to Preferences so session survives app restart
            // NOTE: Session persistence disabled - sessions will NOT survive app restart
            //Preferences.Default.Set("ActiveSessionId", workoutDto.Id);
            //Preferences.Default.Set("ActiveSessionActivityType", workoutDto.ActivityType.ToString());
            //Preferences.Default.Set("ActiveSessionTitle", workoutDto.Title ?? "");
            //Preferences.Default.Set("ActiveSessionStartTime", workoutDto.StartTime.ToUnixTimeMilliseconds());
            //Preferences.Default.Set("ActiveSessionDataOrigin", workoutDto.DataOrigin);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Android ActivityService StartNewSession error");
            return Task.CompletedTask;
        }
    }

    public partial async Task EndActiveSession()
    {
        try
        {
            // COMMENTED OUT: Check memory first, then reconstruct from preferences if needed
            // NOTE: Session persistence disabled - only check memory
            //if (_activeWorkoutDto is null)
            //{
            //    // Try to load from preferences
            //    var activeSessionId = Preferences.Default.Get("ActiveSessionId", string.Empty);
            //    if (!string.IsNullOrEmpty(activeSessionId))
            //    {
            //        var activityTypeStr = Preferences.Default.Get("ActiveSessionActivityType", string.Empty);
            //        var title = Preferences.Default.Get("ActiveSessionTitle", string.Empty);
            //        var startTimeMs = Preferences.Default.Get("ActiveSessionStartTime", 0L);
            //        var dataOrigin = Preferences.Default.Get("ActiveSessionDataOrigin", string.Empty);
            //
            //        if (Enum.TryParse<ActivityType>(activityTypeStr, out var activityType) && startTimeMs > 0)
            //        {
            //            _activeWorkoutDto = new WorkoutDto
            //            {
            //                Id = activeSessionId,
            //                DataOrigin = dataOrigin,
            //                ActivityType = activityType,
            //                Title = string.IsNullOrEmpty(title) ? null : title,
            //                StartTime = DateTimeOffset.FromUnixTimeMilliseconds(startTimeMs),
            //                EndTime = null,
            //                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(startTimeMs)
            //            };
            //        }
            //    }
            //}

            if (_activeWorkoutDto is null)
            {
                _logger?.LogWarning("No active session to end");
                // COMMENTED OUT: Still clear preferences in case there's stale data
                //ClearSessionPreferences();
                return;
            }

            _logger?.LogInformation("Android ActivityService EndActiveSession");

            // Create a new WorkoutDto with updated end time (EndTime is init-only)
            var completedWorkout = new WorkoutDto
            {
                Id = _activeWorkoutDto.Id,
                DataOrigin = _activeWorkoutDto.DataOrigin,
                Timestamp = _activeWorkoutDto.Timestamp,
                ActivityType = _activeWorkoutDto.ActivityType,
                Title = _activeWorkoutDto.Title,
                StartTime = _activeWorkoutDto.StartTime,
                EndTime = DateTimeOffset.UtcNow,
                EnergyBurned = _activeWorkoutDto.EnergyBurned,
                Distance = _activeWorkoutDto.Distance,
                AverageHeartRate = _activeWorkoutDto.AverageHeartRate,
                MaxHeartRate = _activeWorkoutDto.MaxHeartRate,
                MinHeartRate = _activeWorkoutDto.MinHeartRate
            };

            // Write the completed workout (now with valid endTime > startTime)
            await Write(completedWorkout);

            // Clear the active session from memory only
            _activeSession = null;
            _activeWorkoutDto = null;
            // COMMENTED OUT: Clear preferences
            //ClearSessionPreferences();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Android ActivityService EndActiveSession error");
            // COMMENTED OUT: Always clear preferences even if there was an error
            //ClearSessionPreferences();
        }
    }

    // COMMENTED OUT: Clear session preferences helper method
    //private void ClearSessionPreferences()
    //{
    //    Preferences.Default.Remove("ActiveSessionId");
    //    Preferences.Default.Remove("ActiveSessionActivityType");
    //    Preferences.Default.Remove("ActiveSessionTitle");
    //    Preferences.Default.Remove("ActiveSessionStartTime");
    //    Preferences.Default.Remove("ActiveSessionDataOrigin");
    //}
}
