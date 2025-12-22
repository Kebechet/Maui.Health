using Android.Content;
using AndroidX.Health.Connect.Client;
using AndroidX.Health.Connect.Client.Records;
using Maui.Health.Constants;
using Maui.Health.Enums;
using Maui.Health.Extensions;
using Maui.Health.Models;
using Maui.Health.Models.Metrics;
using Maui.Health.Platforms.Android;
using Maui.Health.Platforms.Android.Callbacks;
using Maui.Health.Platforms.Android.Extensions;
using Maui.Health.Platforms.Android.Helpers;
using Microsoft.Extensions.Logging;

namespace Maui.Health.Services;

public partial class HealthWorkoutService
{
    private WorkoutSession? _activeWorkoutSession;
    private readonly ILogger<HealthWorkoutService> _logger;

    private Context _activityContext => Platform.CurrentActivity ??
        throw new Exception("Current activity is null");

    private IHealthConnectClient _healthConnectClient => HealthConnectClient.GetOrCreate(_activityContext);

    public HealthWorkoutService(ILogger<HealthWorkoutService> logger)
    {
        _logger = logger;
    }

    public async partial Task<List<WorkoutDto>> Read(HealthTimeRange activityTime)
    {
        try
        {
            _logger.LogInformation("Android HealthWorkoutService Read: StartTime: {StartTime}, EndTime: {EndTime}",
                activityTime.StartTime, activityTime.EndTime);

            var recordClass = Kotlin.Jvm.JvmClassMappingKt.GetKotlinClass(
                Java.Lang.Class.FromType(typeof(ExerciseSessionRecord)));

            var response = await _healthConnectClient.ReadHealthRecords(recordClass, activityTime);
            if (response is null)
            {
                return [];
            }

            var results = new List<WorkoutDto>();
            foreach (var record in response.Records)
            {
                if (record is not ExerciseSessionRecord exerciseRecord)
                {
                    continue;
                }

                var dto = exerciseRecord.ToWorkoutDto();
                if (dto is not null)
                {
                    results.Add(dto);
                }
            }

            _logger.LogInformation("Android HealthWorkoutService: Found {Count} workout records", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Android HealthWorkoutService Read error");
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
            _logger.LogError(ex, "Android HealthWorkoutService GetActive error");
            return Task.FromResult<WorkoutSession?>(null);
        }
    }

    public async partial Task Write(WorkoutDto workout)
    {
        try
        {
            _logger.LogInformation("Android HealthWorkoutService Write: {ActivityType}", workout.ActivityType);

            var record = workout.ToExerciseSessionRecord();
            if (record is null)
            {
                _logger.LogWarning("Failed to convert WorkoutDto to ExerciseSessionRecord");
                return;
            }

            var success = await _healthConnectClient.InsertRecord(record);
            if (!success)
            {
                _logger.LogWarning("Failed to insert workout record");
                return;
            }

            _logger.LogInformation("Successfully wrote workout record to Health Connect");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Android HealthWorkoutService Write error");
            throw;
        }
    }

    // only activity created by your source can be deleted.
    public async partial Task Delete(WorkoutDto workout)
    {
        try
        {
            _logger.LogInformation("Android HealthWorkoutService Delete: {WorkoutId}", workout.Id);

            var success = await _healthConnectClient.DeleteWorkoutRecord(workout.Id);
            if (!success)
            {
                _logger.LogWarning("Failed to delete workout record");
                return;
            }

            _logger.LogInformation("Successfully deleted workout record from Health Connect");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Android HealthWorkoutService Delete error");
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
            var activeSessionId = Preferences.Default.Get(ActiveSessionStorage.ActiveSessionId, string.Empty);
            return Task.FromResult(!string.IsNullOrEmpty(activeSessionId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Android HealthWorkoutService IsSessionRunning error");
            return Task.FromResult(false);
        }
    }

    public partial Task Start(ActivityType activityType, string? title = null, string? dataOrigin = null)
    {
        try
        {
            _logger.LogInformation("Android HealthWorkoutService StartNewSession: {ActivityType}", activityType);

            var startTime = DateTimeOffset.UtcNow;
            var id = Guid.NewGuid().ToString();
            var origin = dataOrigin ?? _activityContext.PackageName ?? DataOrigin.Unknown;
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
            _logger.LogError(ex, "Android HealthWorkoutService StartNewSession error");
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
                _logger.LogWarning("No active session to pause");
                return Task.CompletedTask;
            }

            if (_activeWorkoutSession.State != WorkoutSessionState.Running)
            {
                _logger.LogWarning("Cannot pause session in state: {State}", _activeWorkoutSession.State);
                return Task.CompletedTask;
            }

            _logger.LogInformation("Android HealthWorkoutService Pause");

            _activeWorkoutSession.Pause();

            SaveWorkoutSessionToPreferences(_activeWorkoutSession);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Android HealthWorkoutService Pause error");
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
                _logger.LogWarning("No active session to resume");
                return Task.CompletedTask;
            }

            if (_activeWorkoutSession.State != WorkoutSessionState.Paused)
            {
                _logger.LogWarning("Cannot resume session in state: {State}", _activeWorkoutSession.State);
                return Task.CompletedTask;
            }

            _logger.LogInformation("Android HealthWorkoutService Resume");

            _activeWorkoutSession.Resume();

            SaveWorkoutSessionToPreferences(_activeWorkoutSession);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Android HealthWorkoutService Resume error");
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
            _logger.LogError(ex, "Android HealthWorkoutService IsPaused error");
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
                _logger.LogWarning("No active session to end");

                ClearSessionPreferences();

                return Task.FromResult<WorkoutDto?>(null);
            }

            _logger.LogInformation("Android HealthWorkoutService EndActiveSession");

            _activeWorkoutSession.End();

            var endTime = DateTimeOffset.UtcNow;
            var totalElapsed = (endTime - _activeWorkoutSession.StartTime).TotalSeconds;
            var activeDuration = totalElapsed - _activeWorkoutSession.TotalPausedSeconds;

            _logger.LogInformation(
                "Android HealthWorkoutService: Total elapsed: {TotalElapsed}s, Paused: {Paused}s, Active: {Active}s",
                totalElapsed, _activeWorkoutSession.TotalPausedSeconds, activeDuration);

            var completedWorkout = _activeWorkoutSession.ToWorkoutDto(endTime);

            _activeWorkoutSession = null;

            ClearSessionPreferences();

            return Task.FromResult<WorkoutDto?>(completedWorkout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Android HealthWorkoutService EndActiveSession error");

            ClearSessionPreferences();

            return Task.FromResult<WorkoutDto?>(null);
        }
    }
}
