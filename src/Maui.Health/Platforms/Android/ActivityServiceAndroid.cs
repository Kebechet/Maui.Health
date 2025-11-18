using Android.Content;
using AndroidX.Health.Connect.Client;
using AndroidX.Health.Connect.Client.Records;
using AndroidX.Health.Connect.Client.Request;
using AndroidX.Health.Connect.Client.Time;
using Java.Time;
using Maui.Health.Constants;
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

            // Try to reconstruct from preferences (for app restarts)
            var activeSessionId = Preferences.Default.Get(SessionPreferenceKeys.ActiveSessionId, string.Empty);
            if (!string.IsNullOrEmpty(activeSessionId))
            {
                var activityTypeStr = Preferences.Default.Get(SessionPreferenceKeys.ActivityType, string.Empty);
                var title = Preferences.Default.Get(SessionPreferenceKeys.Title, string.Empty);
                var startTimeMs = Preferences.Default.Get(SessionPreferenceKeys.StartTime, 0L);
                var dataOrigin = Preferences.Default.Get(SessionPreferenceKeys.DataOrigin, string.Empty);

                if (Enum.TryParse<ActivityType>(activityTypeStr, out var activityType) && startTimeMs > 0)
                {
                    _activeWorkoutDto = new WorkoutDto
                    {
                        Id = activeSessionId,
                        DataOrigin = dataOrigin,
                        ActivityType = activityType,
                        Title = string.IsNullOrEmpty(title) ? null : title,
                        StartTime = DateTimeOffset.FromUnixTimeMilliseconds(startTimeMs),
                        EndTime = null, // Active session - no end time yet
                        Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(startTimeMs)
                    };
                    return Task.FromResult(_activeWorkoutDto);
                }
            }

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

            if(handleField is null)
            {
                _logger?.LogWarning("Failed to get handle field from Health Connect client");
                return;
            }

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

                if(insertMethod is null && clientObject is null)
                {
                    _logger?.LogWarning("Failed to invoke insertRecords method");
                }

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
                _logger?.LogWarning("Failed to get JNI handle for Health Connect client");
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
                                try
                                {
                                    await taskCompletionSource.Task;
                                    _logger?.LogInformation("Successfully deleted workout record from Health Connect");
                                }
                                catch (Exception taskEx)
                                {
                                    // Handle common cases that might occur even when deletion succeeds
                                    if (taskEx.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true ||
                                        taskEx.Message?.Contains("does not exist", StringComparison.OrdinalIgnoreCase) == true)
                                    {
                                        _logger?.LogWarning("Workout record not found (may have been already deleted): {Message}", taskEx.Message);
                                        // Don't throw - treat as successful deletion
                                        return;
                                    }

                                    _logger?.LogError(taskEx, "Error during delete operation");
                                    throw;
                                }
                            }
                        }
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
            // Don't re-throw - log the error but allow the operation to complete
            // This prevents crashes when the record was actually deleted successfully
        }
    }

    public partial Task<bool> IsSessionRunning()
    {
        try
        {
            // Check memory first, then check preferences (for app restarts)
            if (_activeWorkoutDto is not null)
                return Task.FromResult(true);

            // Check if there's a persisted active session
            var activeSessionId = Preferences.Default.Get(SessionPreferenceKeys.ActiveSessionId, string.Empty);
            return Task.FromResult(!string.IsNullOrEmpty(activeSessionId));
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

            // Store the workout DTO for tracking in memory and preferences
            // Don't create ExerciseSessionRecord yet - Android requires endTime > startTime
            // We'll create it when EndActiveSession is called with the actual end time
            _activeWorkoutDto = workoutDto;
            _activeSession = null; // Mark as active but not yet persisted

            // Persist to Preferences so session survives app restart
            Preferences.Default.Set(SessionPreferenceKeys.ActiveSessionId, workoutDto.Id);
            Preferences.Default.Set(SessionPreferenceKeys.ActivityType, workoutDto.ActivityType.ToString());
            Preferences.Default.Set(SessionPreferenceKeys.Title, workoutDto.Title ?? "");
            Preferences.Default.Set(SessionPreferenceKeys.StartTime, workoutDto.StartTime.ToUnixTimeMilliseconds());
            Preferences.Default.Set(SessionPreferenceKeys.DataOrigin, workoutDto.DataOrigin);

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
            // Check memory first, then reconstruct from preferences if needed
            if (_activeWorkoutDto is null)
            {
                // Try to load from preferences
                var activeSessionId = Preferences.Default.Get(SessionPreferenceKeys.ActiveSessionId, string.Empty);
                if (!string.IsNullOrEmpty(activeSessionId))
                {
                    var activityTypeStr = Preferences.Default.Get(SessionPreferenceKeys.ActivityType, string.Empty);
                    var title = Preferences.Default.Get(SessionPreferenceKeys.Title, string.Empty);
                    var startTimeMs = Preferences.Default.Get(SessionPreferenceKeys.StartTime, 0L);
                    var dataOrigin = Preferences.Default.Get(SessionPreferenceKeys.DataOrigin, string.Empty);

                    if (Enum.TryParse<ActivityType>(activityTypeStr, out var activityType) && startTimeMs > 0)
                    {
                        _activeWorkoutDto = new WorkoutDto
                        {
                            Id = activeSessionId,
                            DataOrigin = dataOrigin,
                            ActivityType = activityType,
                            Title = string.IsNullOrEmpty(title) ? null : title,
                            StartTime = DateTimeOffset.FromUnixTimeMilliseconds(startTimeMs),
                            EndTime = null,
                            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(startTimeMs)
                        };
                    }
                }
            }

            if (_activeWorkoutDto is null)
            {
                _logger?.LogWarning("No active session to end");
                // Still clear preferences in case there's stale data
                ClearSessionPreferences();
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

            // Clear the active session from memory and preferences
            _activeSession = null;
            _activeWorkoutDto = null;
            ClearSessionPreferences();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Android ActivityService EndActiveSession error");
            // Always clear preferences even if there was an error
            ClearSessionPreferences();
        }
    }

    private void ClearSessionPreferences()
    {
        Preferences.Default.Remove(SessionPreferenceKeys.ActiveSessionId);
        Preferences.Default.Remove(SessionPreferenceKeys.ActivityType);
        Preferences.Default.Remove(SessionPreferenceKeys.Title);
        Preferences.Default.Remove(SessionPreferenceKeys.StartTime);
        Preferences.Default.Remove(SessionPreferenceKeys.DataOrigin);
    }
}
