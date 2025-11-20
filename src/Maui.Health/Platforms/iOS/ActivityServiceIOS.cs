using Foundation;
using HealthKit;
using Maui.Health.Constants;
using Maui.Health.Enums;
using Maui.Health.Extensions;
using Maui.Health.Models;
using Maui.Health.Models.Metrics;
using Maui.Health.Platforms.iOS.Extensions;
using Microsoft.Extensions.Logging;
using static Maui.Health.Constants.HealthConstants;

namespace Maui.Health.Services;

public partial class ActivityService
{
    private WorkoutSession? _activeWorkoutSession;
    private readonly ILogger<ActivityService>? _logger;
    private nuint _healthRateLimit { get; set; } = Defaults.HealthRateLimit;

    public ActivityService(ILogger<ActivityService>? logger = null)
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

            _logger?.LogInformation("iOS ActivityService Read: StartTime: {StartTime}, EndTime: {EndTime}",
                activityTime.StartTime, activityTime.EndTime);

            // Request read permission for workouts before querying
            var permissionGranted = await RequestWorkoutReadPermission();
            if (!permissionGranted)
            {
                _logger?.LogWarning("iOS ActivityService: Workout read permission not granted");
                return [];
            }

            // Use DateTimeOffset.UtcDateTime for correct timezone handling
            var predicate = HKQuery.GetPredicateForSamples(
                activityTime.StartTime.ToNSDate(),
                activityTime.EndTime.ToNSDate(),
                HKQueryOptions.StrictStartDate
            );

            var tcs = new TaskCompletionSource<HKWorkout[]>();
            var workoutType = HKWorkoutType.WorkoutType;

            var query = new HKSampleQuery(
                workoutType,
                predicate,
                _healthRateLimit,
                [new NSSortDescriptor(HKSample.SortIdentifierStartDate, false)],
                (sampleQuery, results, error) =>
                {
                    if (error != null)
                    {
                        tcs.TrySetResult([]);
                        return;
                    }

                    var workouts = results?.OfType<HKWorkout>().ToArray();
                    tcs.TrySetResult(workouts ?? []);
                }
            );

            using var store = new HKHealthStore();
            store.ExecuteQuery(query);
            var workouts = await tcs.Task;

            var dtos = new List<WorkoutDto>();
            foreach (var workout in workouts)
            {
                WorkoutDto? dto;
                if (HeartRateQueryCallback != null)
                {
                    dto = await workout.ToWorkoutDtoAsync(
                        async (range, ct) => (await HeartRateQueryCallback(range, ct)).ToArray(),
                        CancellationToken.None);
                }
                else
                {
                    dto = workout.ToWorkoutDto();
                }

                if (dto is not null)
                {
                    dtos.Add(dto);
                }
            }

            _logger?.LogInformation("iOS ActivityService: Found {Count} workout records", dtos.Count);
            return dtos;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "iOS ActivityService Read error");
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
            _logger?.LogError(ex, "iOS ActivityService ReadActive error");
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

            _logger?.LogInformation("iOS ActivityService Write: {ActivityType}", workout.ActivityType);

            var hkWorkout = workout.ToHKWorkout();
            if (hkWorkout == null)
            {
                _logger?.LogWarning("Failed to convert WorkoutDto to HKWorkout");
                return;
            }

            using var healthStore = new HKHealthStore();
            var tcs = new TaskCompletionSource<bool>();

            healthStore.SaveObject(hkWorkout, (success, error) =>
            {
                if (error != null)
                {
                    _logger?.LogError("iOS ActivityService Write Error: {Error}", error.LocalizedDescription);
                    tcs.TrySetResult(false);
                    return;
                }

                _logger?.LogInformation("iOS ActivityService: Successfully wrote workout");
                tcs.TrySetResult(success);
            });

            await tcs.Task;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "iOS ActivityService Write error");
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

            _logger?.LogInformation("iOS ActivityService Delete: {WorkoutId}", workout.Id);

            // To delete a workout from HealthKit, we need to query for it first using its UUID
            // The workout.Id should be the UUID string from HealthKit
            if (!Guid.TryParse(workout.Id, out var workoutGuid))
            {
                _logger?.LogWarning("Invalid workout ID format for deletion: {WorkoutId}", workout.Id);
                return;
            }

            var uuid = new NSUuid(workoutGuid.ToString());
            var predicate = HKQuery.GetPredicateForObject(uuid);
            var workoutType = HKWorkoutType.WorkoutType;

            var tcs = new TaskCompletionSource<HKWorkout?>();
            var query = new HKSampleQuery(
                workoutType,
                predicate,
                Defaults.SingleRecordLimit,
                null,
                (sampleQuery, results, error) =>
                {
                    if (error != null)
                    {
                        _logger?.LogError("iOS ActivityService Delete query error: {Error}", error.LocalizedDescription);
                        tcs.TrySetResult(null);
                        return;
                    }

                    var workoutToDelete = results?.FirstOrDefault() as HKWorkout;
                    tcs.TrySetResult(workoutToDelete);
                }
            );

            using var healthStore = new HKHealthStore();
            healthStore.ExecuteQuery(query);
            var hkWorkout = await tcs.Task;

            if (hkWorkout == null)
            {
                _logger?.LogWarning("Workout not found for deletion: {WorkoutId}", workout.Id);
                return;
            }

            var deleteTcs = new TaskCompletionSource<bool>();
            healthStore.DeleteObject(hkWorkout, (success, error) =>
            {
                if (error != null)
                {
                    _logger?.LogError("iOS ActivityService Delete error: {Error}", error.LocalizedDescription);
                    deleteTcs.TrySetResult(false);
                    return;
                }

                _logger?.LogInformation("iOS ActivityService: Successfully deleted workout");
                deleteTcs.TrySetResult(success);
            });

            await deleteTcs.Task;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "iOS ActivityService Delete error");
            throw; // Re-throw so caller knows it failed
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
            var activeSessionId = Preferences.Default.Get(SessionPreferenceKeys.ActiveSessionId, string.Empty);
            return Task.FromResult(!string.IsNullOrEmpty(activeSessionId));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "iOS ActivityService IsSessionRunning error");
            return Task.FromResult(false);
        }
    }

    public partial Task Start(ActivityType activityType, string? title = null, string? dataOrigin = null)
    {
        try
        {
            _logger?.LogInformation("iOS ActivityService StartNewSession: {ActivityType}", activityType);

            if (!HKHealthStore.IsHealthDataAvailable)
            {
                return Task.CompletedTask;
            }

            var startTime = DateTimeOffset.UtcNow;
            var id = Guid.NewGuid().ToString();
            var origin = dataOrigin ?? DataOrigins.HealthKit;
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
            _logger?.LogError(ex, "iOS ActivityService StartNewSession error");
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

            _logger?.LogInformation("iOS ActivityService Pause");

            _activeWorkoutSession.Pause();

            SaveWorkoutSessionToPreferences(_activeWorkoutSession);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "iOS ActivityService Pause error");
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

            _logger?.LogInformation("iOS ActivityService Resume");

            _activeWorkoutSession.Resume();

            SaveWorkoutSessionToPreferences(_activeWorkoutSession);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "iOS ActivityService Resume error");
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
            _logger?.LogError(ex, "iOS ActivityService IsPaused error");
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
                // Still clear preferences in case there's stale data
                ClearSessionPreferences();
                return Task.FromResult<WorkoutDto?>(null);
            }

            _logger?.LogInformation("iOS ActivityService EndActiveSession");

            _activeWorkoutSession.End();

            var endTime = DateTimeOffset.UtcNow;
            var totalElapsed = (endTime - _activeWorkoutSession.StartTime).TotalSeconds;
            var activeDuration = totalElapsed - _activeWorkoutSession.TotalPausedSeconds;

            _logger?.LogInformation(
                "iOS ActivityService: Total elapsed: {TotalElapsed}s, Paused: {Paused}s, Active: {Active}s",
                totalElapsed, _activeWorkoutSession.TotalPausedSeconds, activeDuration);

            var completedWorkout = _activeWorkoutSession.ToWorkoutDto(endTime);

            _activeWorkoutSession = null;
            ClearSessionPreferences();

            return Task.FromResult<WorkoutDto?>(completedWorkout);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "iOS ActivityService EndActiveSession error");
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
                _logger?.LogError("iOS ActivityService: Permission request error: {Error}", error.LocalizedDescription);
                return false;
            }

            _logger?.LogInformation("iOS ActivityService: Workout read permission granted: {Success}", success);
            return success;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "iOS ActivityService: Error requesting workout read permission");
            return false;
        }
    }
}
