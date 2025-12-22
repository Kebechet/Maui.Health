using Foundation;
using HealthKit;
using Maui.Health.Constants;
using Maui.Health.Enums;
using Maui.Health.Extensions;
using Maui.Health.Models;
using Maui.Health.Models.Metrics;
using Maui.Health.Platforms.iOS.Extensions;
using Microsoft.Extensions.Logging;

namespace Maui.Health.Services;

public partial class HealthWorkoutService
{
    private WorkoutSession? _activeWorkoutSession;
    private readonly ILogger<HealthWorkoutService> _logger;

    public HealthWorkoutService(ILogger<HealthWorkoutService> logger)
    {
        _logger = logger;
    }

    public async partial Task<List<WorkoutDto>> Read(HealthTimeRange activityTime)
    {
        try
        {
            if (!HKHealthStore.IsHealthDataAvailable)
            {
                return [];
            }

            _logger.LogInformation("iOS HealthWorkoutService Read: StartTime: {StartTime}, EndTime: {EndTime}",
                activityTime.StartTime, activityTime.EndTime);

            var permissionGranted = await RequestWorkoutReadPermission();
            if (!permissionGranted)
            {
                _logger.LogWarning("iOS HealthWorkoutService: Workout read permission not granted");
                return [];
            }

            using var store = new HKHealthStore();
            var workouts = await store.ReadWorkouts(activityTime);

            var dtos = new List<WorkoutDto>();
            foreach (var workout in workouts)
            {
                var dto = workout.ToWorkoutDto();
                if (dto is not null)
                {
                    dtos.Add(dto);
                }
            }

            _logger.LogInformation("iOS HealthWorkoutService: Found {Count} workout records", dtos.Count);
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "iOS HealthWorkoutService Read error");
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
            _logger.LogError(ex, "iOS HealthWorkoutService ReadActive error");
            return Task.FromResult<WorkoutSession?>(null);
        }
    }

    public async partial Task Write(WorkoutDto workout)
    {
        try
        {
            if (!HKHealthStore.IsHealthDataAvailable)
            {
                return;
            }

            _logger.LogInformation("iOS HealthWorkoutService Write: {ActivityType}", workout.ActivityType);

            var hkWorkout = workout.ToHKWorkout();
            if (hkWorkout is null)
            {
                _logger.LogWarning("Failed to convert WorkoutDto to HKWorkout");
                return;
            }

            using var healthStore = new HKHealthStore();
            var success = await healthStore.Save(hkWorkout);

            if (!success)
            {
                _logger.LogWarning("iOS HealthWorkoutService: Failed to write workout");
                return;
            }

            _logger.LogInformation("iOS HealthWorkoutService: Successfully wrote workout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "iOS HealthWorkoutService Write error");
        }
    }

    public async partial Task Delete(WorkoutDto workout)
    {
        try
        {
            if (!HKHealthStore.IsHealthDataAvailable)
            {
                return;
            }

            _logger.LogInformation("iOS HealthWorkoutService Delete: {WorkoutId}", workout.Id);

            using var healthStore = new HKHealthStore();

            var hkWorkout = await healthStore.FindWorkoutById(workout.Id);
            if (hkWorkout is null)
            {
                _logger.LogWarning("Workout not found for deletion: {WorkoutId}", workout.Id);
                return;
            }

            var success = await healthStore.Delete(hkWorkout);
            if (!success)
            {
                _logger.LogWarning("iOS HealthWorkoutService: Failed to delete workout");
                return;
            }

            _logger.LogInformation("iOS HealthWorkoutService: Successfully deleted workout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "iOS HealthWorkoutService Delete error");
            throw;
        }
    }

    public partial Task<bool> IsRunning()
    {
        try
        {
            // Check memory first, then check preferences (for app restarts)
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
            _logger.LogError(ex, "iOS HealthWorkoutService IsSessionRunning error");
            return Task.FromResult(false);
        }
    }

    public partial Task Start(ActivityType activityType, string? title = null, string? dataOrigin = null)
    {
        try
        {
            _logger.LogInformation("iOS HealthWorkoutService StartNewSession: {ActivityType}", activityType);

            if (!HKHealthStore.IsHealthDataAvailable)
            {
                return Task.CompletedTask;
            }

            _activeWorkoutSession = new WorkoutSession(
                Guid.NewGuid().ToString(),
                activityType,
                title ?? activityType.ToString(),
                dataOrigin ?? DataOrigin.HealthKitOrigin,
                DateTimeOffset.UtcNow,
                WorkoutSessionState.Running
            );

            SaveWorkoutSessionToPreferences(_activeWorkoutSession);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "iOS HealthWorkoutService StartNewSession error");
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

            _logger.LogInformation("iOS HealthWorkoutService Pause");

            _activeWorkoutSession.Pause();

            SaveWorkoutSessionToPreferences(_activeWorkoutSession);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "iOS HealthWorkoutService Pause error");
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

            _logger.LogInformation("iOS HealthWorkoutService Resume");

            _activeWorkoutSession.Resume();

            SaveWorkoutSessionToPreferences(_activeWorkoutSession);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "iOS HealthWorkoutService Resume error");
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
            _logger.LogError(ex, "iOS HealthWorkoutService IsPaused error");
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
                // Still clear preferences in case there's stale data
                ClearSessionPreferences();
                return Task.FromResult<WorkoutDto?>(null);
            }

            _logger.LogInformation("iOS HealthWorkoutService EndActiveSession");

            _activeWorkoutSession.End();

            var endTime = DateTimeOffset.UtcNow;
            var totalElapsed = (endTime - _activeWorkoutSession.StartTime).TotalSeconds;
            var activeDuration = totalElapsed - _activeWorkoutSession.TotalPausedSeconds;

            _logger.LogInformation(
                "iOS HealthWorkoutService: Total elapsed: {TotalElapsed}s, Paused: {Paused}s, Active: {Active}s",
                totalElapsed, _activeWorkoutSession.TotalPausedSeconds, activeDuration);

            var completedWorkout = _activeWorkoutSession.ToWorkoutDto(endTime);

            _activeWorkoutSession = null;
            ClearSessionPreferences();

            return Task.FromResult<WorkoutDto?>(completedWorkout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "iOS HealthWorkoutService EndActiveSession error");
            // Always clear preferences even if there was an error
            ClearSessionPreferences();

            return Task.FromResult<WorkoutDto?>(null);
        }
    }

    /// <summary>
    /// Requests read permission for workouts from HealthKit
    /// </summary>
    /// <returns>True if permission was granted, false otherwise</returns>
    private async Task<bool> RequestWorkoutReadPermission()
    {
        try
        {
            var workoutType = HKWorkoutType.WorkoutType;
            var readTypes = new NSSet<HKObjectType>(workoutType);
            var writeTypes = new NSSet<HKObjectType>();

            using var healthStore = new HKHealthStore();

            var (success, error) = await healthStore.RequestAuthorizationToShareAsync(writeTypes, readTypes);

            if (error != null)
            {
                _logger.LogError("iOS HealthWorkoutService: Permission request error: {Error}", error.LocalizedDescription);
                return false;
            }

            _logger.LogInformation("iOS HealthWorkoutService: Workout read permission granted: {Success}", success);
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "iOS HealthWorkoutService: Error requesting workout read permission");
            return false;
        }
    }
}
