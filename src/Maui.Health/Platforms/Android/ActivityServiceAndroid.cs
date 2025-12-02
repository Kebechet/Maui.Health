using Android.Content;
using AndroidX.Health.Connect.Client;
using AndroidX.Health.Connect.Client.Records;
using AndroidX.Health.Connect.Client.Request;
using AndroidX.Health.Connect.Client.Time;
using Java.Time;
using Maui.Health.Constants;
using Maui.Health.Enums;
using Maui.Health.Extensions;
using Maui.Health.Models;
using Maui.Health.Models.Metrics;
using Maui.Health.Platforms.Android;
using Maui.Health.Platforms.Android.Callbacks;
using Maui.Health.Platforms.Android.Extensions;
using Microsoft.Extensions.Logging;
using static Maui.Health.Constants.HealthConstants;

namespace Maui.Health.Services;

public partial class ActivityService
{
    private WorkoutSession? _activeWorkoutSession;
    private readonly ILogger<ActivityService>? _logger;

    private Context _activityContext => Platform.CurrentActivity ??
        throw new Exception("Current activity is null");

    private IHealthConnectClient _healthConnectClient => HealthConnectClient.GetOrCreate(_activityContext);

    public ActivityService(ILogger<ActivityService>? logger = null)
    {
        _logger = logger;
    }

    public async partial Task<List<WorkoutDto>> Read(HealthTimeRange activityTime)
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
                AndroidConstants.MaxRecordsPerRequest,
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
                if (record is not ExerciseSessionRecord exerciseRecord)
                {
                    continue;
                }

                WorkoutDto? dto;
                if (HeartRateQueryCallback != null || CaloriesQueryCallback != null)
                {
                    dto = await exerciseRecord.ToWorkoutDtoAsync(
                        HeartRateQueryCallback != null
                            ? async (range, ct) => (await HeartRateQueryCallback(range, ct)).ToArray()
                            : null,
                        CaloriesQueryCallback != null
                            ? async (range, ct) => (await CaloriesQueryCallback(range, ct)).ToArray()
                            : null,
                        CancellationToken.None);
                }
                else
                {
                    dto = exerciseRecord.ToWorkoutDto();
                }

                if (dto is not null)
                {
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

    public partial Task<WorkoutSession?> GetActive()
    {
        try
        {
            // Try to load from preferences if not in memory
            _activeWorkoutSession ??= LoadWorkoutSessionFromPreferences();

            return Task.FromResult(_activeWorkoutSession);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Android ActivityService ReadActive error");
            return Task.FromResult<WorkoutSession?>(null);
        }
    }

    public async partial Task Write(WorkoutDto workout)
    {
        try
        {
            _logger?.LogInformation("Android ActivityService Write: {ActivityType}", workout.ActivityType);

            var record = workout.ToExerciseSessionRecord();
            if (record is null)
            {
                _logger?.LogWarning("Failed to convert WorkoutDto to ExerciseSessionRecord");
                return;
            }

            var recordsList = new Java.Util.ArrayList();
            recordsList.Add(record);

            var clientType = _healthConnectClient.GetType();
            var handleField = clientType.GetField(AndroidConstants.JniHandleFieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (handleField is null)
            {
                _logger?.LogWarning("Failed to get handle field from Health Connect client");
                return;
            }

            var handle = handleField.GetValue(_healthConnectClient);
            if (handle is not IntPtr jniHandle || jniHandle == IntPtr.Zero)
            {
                _logger?.LogWarning("Failed to get JNI handle for Health Connect client");
                return;
            }

            var classHandle = Android.Runtime.JNIEnv.GetObjectClass(jniHandle);
            var clientClass = Java.Lang.Object.GetObject<Java.Lang.Class>(
                classHandle, Android.Runtime.JniHandleOwnership.DoNotTransfer);

            var clientObject = Java.Lang.Object.GetObject<Java.Lang.Object>(
                jniHandle, Android.Runtime.JniHandleOwnership.DoNotTransfer);

            var insertMethod = clientClass?.GetDeclaredMethod("insertRecords",
                Java.Lang.Class.FromType(typeof(Java.Util.IList)),
                Java.Lang.Class.FromType(typeof(Kotlin.Coroutines.IContinuation)));

            if (insertMethod is null || clientObject is null)
            {
                _logger?.LogWarning("Failed to invoke insertRecords method");
                return;
            }

            var taskCompletionSource = new TaskCompletionSource<Java.Lang.Object>();
            var continuation = new Continuation(taskCompletionSource, default);

            insertMethod.Accessible = true;
            var result = insertMethod.Invoke(clientObject, recordsList, continuation);

            if (result is not Java.Lang.Enum javaEnum)
            {
                _logger?.LogInformation("Successfully wrote workout record to Health Connect");
                return;
            }

            var currentState = Enum.Parse<CoroutineState>(javaEnum.ToString());
            if (currentState == CoroutineState.COROUTINE_SUSPENDED)
            {
                await taskCompletionSource.Task;
            }

            _logger?.LogInformation("Successfully wrote workout record to Health Connect");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Android ActivityService Write error");
            throw; // Re-throw so caller knows it failed
        }
    }

    // only aktivity created by your source can be deleted.
    public async partial Task Delete(WorkoutDto workout)
    {
        try
        {
            _logger?.LogInformation("Android ActivityService Delete: {WorkoutId}", workout.Id);

            // Create a list with the workout ID to delete
            var recordIdsList = new Java.Util.ArrayList();
            recordIdsList.Add(workout.Id);

            var clientType = _healthConnectClient.GetType();
            var handleField = clientType.GetField(AndroidConstants.JniHandleFieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (handleField is null)
            {
                _logger?.LogWarning("Failed to get handle field from Health Connect client");
                return;
            }

            var handle = handleField.GetValue(_healthConnectClient);
            if (handle is not IntPtr jniHandle || jniHandle == IntPtr.Zero)
            {
                _logger?.LogWarning("Failed to get JNI handle for Health Connect client");
                return;
            }

            var classHandle = global::Android.Runtime.JNIEnv.GetObjectClass(jniHandle);
            var clientClass = Java.Lang.Object.GetObject<Java.Lang.Class>(
                classHandle, global::Android.Runtime.JniHandleOwnership.DoNotTransfer);

            var clientObject = Java.Lang.Object.GetObject<Java.Lang.Object>(
                jniHandle, global::Android.Runtime.JniHandleOwnership.DoNotTransfer);

            var recordClass = Kotlin.Jvm.JvmClassMappingKt.GetKotlinClass(
                Java.Lang.Class.FromType(typeof(ExerciseSessionRecord)));

            var deleteMethod = clientClass?.GetDeclaredMethod("deleteRecords",
                Java.Lang.Class.FromType(typeof(Kotlin.Reflect.IKClass)),
                Java.Lang.Class.FromType(typeof(Java.Util.IList)),
                Java.Lang.Class.FromType(typeof(Java.Util.IList)),
                Java.Lang.Class.FromType(typeof(Kotlin.Coroutines.IContinuation)));

            if (deleteMethod is null || clientObject is null)
            {
                _logger?.LogWarning("Failed to invoke deleteRecords method");
                return;
            }

            var taskCompletionSource = new TaskCompletionSource<Java.Lang.Object>();
            var continuation = new Continuation(taskCompletionSource, default);

            deleteMethod.Accessible = true;
            var emptyList = new Java.Util.ArrayList(); // Empty client record ID list

            var recordClassObj = Java.Lang.Object.GetObject<Java.Lang.Object>(
                recordClass.Handle, Android.Runtime.JniHandleOwnership.DoNotTransfer);

            var result = deleteMethod.Invoke(clientObject, recordClassObj, recordIdsList, emptyList, continuation);

            if (result is not Java.Lang.Enum javaEnum)
            {
                return;
            }

            var currentState = Enum.Parse<CoroutineState>(javaEnum.ToString());
            if (currentState != CoroutineState.COROUTINE_SUSPENDED)
            {
                return;
            }

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
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Android ActivityService Delete error");
            // Don't re-throw - log the error but allow the operation to complete
            // This prevents crashes when the record was actually deleted successfully
        }
    }

    public partial Task<bool> IsRunning()
    {
        try
        {
            if (_activeWorkoutSession is not null)
            {
                return Task.FromResult(true);
            }

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

    public partial Task Start(ActivityType activityType, string? title = null, string? dataOrigin = null)
    {
        try
        {
            _logger?.LogInformation("Android ActivityService StartNewSession: {ActivityType}", activityType);

            var startTime = DateTimeOffset.UtcNow;
            var id = Guid.NewGuid().ToString();
            var origin = dataOrigin ?? _activityContext.PackageName ?? "Unknown";
            var workoutTitle = title ?? activityType.ToString();

            _activeWorkoutSession = new WorkoutSession(
                id,
                activityType,
                workoutTitle,
                origin,
                startTime,
                WorkoutSessionState.Running
            );

            SaveWorkoutSessionToPreferences(_activeWorkoutSession);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Android ActivityService StartNewSession error");
            return Task.CompletedTask;
        }
    }

    public partial Task Pause()
    {
        try
        {
            _activeWorkoutSession ??= LoadWorkoutSessionFromPreferences();

            if (_activeWorkoutSession is null)
            {
                _logger?.LogWarning("No active session to pause");
                return Task.CompletedTask;
            }

            if (_activeWorkoutSession.State != WorkoutSessionState.Running)
            {
                _logger?.LogWarning("Cannot pause session in state: {State}", _activeWorkoutSession.State);
                return Task.CompletedTask;
            }

            _logger?.LogInformation("Android ActivityService Pause");

            _activeWorkoutSession.Pause();

            SaveWorkoutSessionToPreferences(_activeWorkoutSession);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Android ActivityService Pause error");
            return Task.CompletedTask;
        }
    }

    public partial Task Resume()
    {
        try
        {
            _activeWorkoutSession ??= LoadWorkoutSessionFromPreferences();

            if (_activeWorkoutSession is null)
            {
                _logger?.LogWarning("No active session to resume");
                return Task.CompletedTask;
            }

            if (_activeWorkoutSession.State != WorkoutSessionState.Paused)
            {
                _logger?.LogWarning("Cannot resume session in state: {State}", _activeWorkoutSession.State);
                return Task.CompletedTask;
            }

            _logger?.LogInformation("Android ActivityService Resume");

            _activeWorkoutSession.Resume();

            SaveWorkoutSessionToPreferences(_activeWorkoutSession);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Android ActivityService Resume error");
            return Task.CompletedTask;
        }
    }

    public partial Task<bool> IsPaused()
    {
        try
        {
            _activeWorkoutSession ??= LoadWorkoutSessionFromPreferences();

            if (_activeWorkoutSession is null)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(_activeWorkoutSession.State == WorkoutSessionState.Paused);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Android ActivityService IsPaused error");
            return Task.FromResult(false);
        }
    }

    public partial Task<WorkoutDto?> End()
    {
        try
        {
            _activeWorkoutSession ??= LoadWorkoutSessionFromPreferences();

            if (_activeWorkoutSession is null)
            {
                _logger?.LogWarning("No active session to end");

                ClearSessionPreferences();

                return Task.FromResult<WorkoutDto?>(null);
            }

            _logger?.LogInformation("Android ActivityService EndActiveSession");

            _activeWorkoutSession.End();

            var endTime = DateTimeOffset.UtcNow;
            var totalElapsed = (endTime - _activeWorkoutSession.StartTime).TotalSeconds;
            var activeDuration = totalElapsed - _activeWorkoutSession.TotalPausedSeconds;

            _logger?.LogInformation(
                "Android ActivityService: Total elapsed: {TotalElapsed}s, Paused: {Paused}s, Active: {Active}s",
                totalElapsed, _activeWorkoutSession.TotalPausedSeconds, activeDuration);

            var completedWorkout = _activeWorkoutSession.ToWorkoutDto(endTime);

            _activeWorkoutSession = null;

            ClearSessionPreferences();

            return Task.FromResult<WorkoutDto?>(completedWorkout);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Android ActivityService EndActiveSession error");

            ClearSessionPreferences();

            return Task.FromResult<WorkoutDto?>(null);
        }
    }
}
