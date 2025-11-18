using Foundation;
using HealthKit;
using Maui.Health.Constants;
using Maui.Health.Enums;
using Maui.Health.Models.Metrics;
using Maui.Health.Platforms.iOS.Extensions;
using Microsoft.Extensions.Logging;

namespace Maui.Health.Services;

public partial class ActivityService
{
    private WorkoutDto? _activeWorkoutDto;
    private readonly ILogger<ActivityService>? _logger;
    private nuint _healthRateLimit { get; set; } = 0;

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

            // Ensure DateTime is treated as UTC for NSDate conversion
            var fromUtc = DateTime.SpecifyKind(activityTime.StartDateTime, DateTimeKind.Utc);
            var toUtc = DateTime.SpecifyKind(activityTime.EndDateTime, DateTimeKind.Utc);

            var predicate = HKQuery.GetPredicateForSamples(
                (NSDate)fromUtc,
                (NSDate)toUtc,
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
                var dto = await workout.ToWorkoutDtoAsync(QueryHeartRateSamplesAsync, CancellationToken.None);
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
            _logger?.LogError(ex, "iOS ActivityService ReadActive error");
            return Task.FromResult<WorkoutDto>(null!);
        }
    }

    public partial async Task Write(WorkoutDto workout)
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
                }
                else
                {
                    _logger?.LogInformation("iOS ActivityService: Successfully wrote workout");
                    tcs.TrySetResult(success);
                }
            });

            await tcs.Task;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "iOS ActivityService Write error");
        }
    }

    public partial async Task Delete(WorkoutDto workout)
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
                1,
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
                }
                else
                {
                    _logger?.LogInformation("iOS ActivityService: Successfully deleted workout");
                    deleteTcs.TrySetResult(success);
                }
            });

            await deleteTcs.Task;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "iOS ActivityService Delete error");
            throw; // Re-throw so caller knows it failed
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
            _logger?.LogError(ex, "iOS ActivityService IsSessionRunning error");
            return Task.FromResult(false);
        }
    }

    public partial Task StartNewSession(WorkoutDto workoutDto)
    {
        try
        {
            _logger?.LogInformation("iOS ActivityService StartNewSession: {ActivityType}", workoutDto.ActivityType);

            if (!HKHealthStore.IsHealthDataAvailable)
            {
                return Task.CompletedTask;
            }

            // For iOS (not watchOS), we track the workout locally in memory and preferences
            // HKWorkoutSession is primarily for watchOS real-time tracking
            _activeWorkoutDto = workoutDto;

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
            _logger?.LogError(ex, "iOS ActivityService StartNewSession error");
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

            _logger?.LogInformation("iOS ActivityService EndActiveSession");

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

            // Write the completed workout
            await Write(completedWorkout);

            // Clear the active session from memory and preferences
            _activeWorkoutDto = null;
            ClearSessionPreferences();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "iOS ActivityService EndActiveSession error");
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

    private async Task<HeartRateDto[]> QueryHeartRateSamplesAsync(HealthTimeRange timeRange, CancellationToken cancellationToken)
    {
        try
        {
            var fromUtc = DateTime.SpecifyKind(timeRange.StartDateTime, DateTimeKind.Utc);
            var toUtc = DateTime.SpecifyKind(timeRange.EndDateTime, DateTimeKind.Utc);

            var quantityType = HKQuantityType.Create(HKQuantityTypeIdentifier.HeartRate)!;
            var predicate = HKQuery.GetPredicateForSamples((NSDate)fromUtc, (NSDate)toUtc, HKQueryOptions.StrictStartDate);
            var tcs = new TaskCompletionSource<HeartRateDto[]>();

            var query = new HKSampleQuery(
                quantityType,
                predicate,
                _healthRateLimit,
                new[] { new NSSortDescriptor(HKSample.SortIdentifierStartDate, false) },
                (sampleQuery, results, error) =>
                {
                    if (error != null)
                    {
                        tcs.TrySetResult([]);
                        return;
                    }

                    var dtos = new List<HeartRateDto>();
                    foreach (var sample in results?.OfType<HKQuantitySample>() ?? [])
                    {
                        var dto = sample.ToHeartRateDto();
                        dtos.Add(dto);
                    }

                    tcs.TrySetResult(dtos.ToArray());
                }
            );

            using var store = new HKHealthStore();
            using var ct = cancellationToken.Register(() =>
            {
                tcs.TrySetCanceled();
                store.StopQuery(query);
            });

            store.ExecuteQuery(query);
            return await tcs.Task;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error querying heart rate samples");
            return [];
        }
    }
}
