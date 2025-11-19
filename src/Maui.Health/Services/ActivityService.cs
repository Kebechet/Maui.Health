using Maui.Health.Constants;
using Maui.Health.Enums;
using Maui.Health.Models.Metrics;

namespace Maui.Health.Services;

public partial class ActivityService
{
    /// <summary>
    /// Callback to fetch heart rate data for a time range (set by HealthService)
    /// </summary>
    internal Func<HealthTimeRange, CancellationToken, Task<List<HeartRateDto>>>? HeartRateQueryCallback { get; set; }

    /// <summary>
    /// Callback to fetch active calories data for a time range (set by HealthService)
    /// </summary>
    internal Func<HealthTimeRange, CancellationToken, Task<List<ActiveCaloriesBurnedDto>>>? CaloriesQueryCallback { get; set; }

    public partial Task<List<WorkoutDto>> Read(HealthTimeRange activityTime);

    public partial Task Write(WorkoutDto workout);

    /// <summary>
    /// Deletes a workout record from the health data store
    /// </summary>
    /// <param name="workout">The workout record to delete</param>
    /// <remarks>
    /// Note: You can only delete workout records that were created by your application.
    /// Attempting to delete workouts created by other apps will fail or be ignored by the platform.
    /// </remarks>
    public partial Task Delete(WorkoutDto workout);
    public partial Task<WorkoutDto> GetActive(HealthTimeRange activityTime);

    public partial Task<bool> IsRunning();

    public partial Task Start(WorkoutDto workoutDto);

    public partial Task Pause();

    public partial Task Resume();

    public partial Task<bool> IsPaused();

    public partial Task End();

    /// <summary>
    /// Loads the active workout session from preferences (used for app restarts)
    /// </summary>
    protected WorkoutDto? LoadActiveWorkoutFromPreferences()
    {
        var session = LoadWorkoutSessionFromPreferences();
        if (session == null)
        {
            return null;
        }

        return new WorkoutDto
        {
            Id = session.Id,
            DataOrigin = session.DataOrigin,
            ActivityType = session.ActivityType,
            Title = session.Title,
            StartTime = session.StartTime,
            EndTime = null,
            Timestamp = session.StartTime
        };
    }

    /// <summary>
    /// Loads the active workout session object from preferences (used for app restarts)
    /// </summary>
    protected Models.WorkoutSession? LoadWorkoutSessionFromPreferences()
    {
        var activeSessionId = Preferences.Default.Get(SessionPreferenceKeys.ActiveSessionId, string.Empty);
        if (string.IsNullOrEmpty(activeSessionId))
        {
            return null;
        }

        var activityTypeStr = Preferences.Default.Get(SessionPreferenceKeys.ActivityType, string.Empty);
        var title = Preferences.Default.Get(SessionPreferenceKeys.Title, string.Empty);
        var startTimeMs = Preferences.Default.Get(SessionPreferenceKeys.StartTime, 0L);
        var dataOrigin = Preferences.Default.Get(SessionPreferenceKeys.DataOrigin, string.Empty);
        var stateStr = Preferences.Default.Get(SessionPreferenceKeys.State, string.Empty);
        var pauseIntervalsJson = Preferences.Default.Get(SessionPreferenceKeys.PauseIntervals, string.Empty);

        if (!System.Enum.TryParse<ActivityType>(activityTypeStr, out var activityType) || startTimeMs <= 0)
        {
            return null;
        }

        if (!System.Enum.TryParse<WorkoutSessionState>(stateStr, out var state))
        {
            state = WorkoutSessionState.Running;
        }

        // Parse pause intervals from JSON
        var pauseIntervals = new List<(DateTimeOffset, DateTimeOffset?)>();
        if (!string.IsNullOrEmpty(pauseIntervalsJson))
        {
            try
            {
                var intervals = System.Text.Json.JsonSerializer.Deserialize<List<(long, long?)>>(pauseIntervalsJson);
                if (intervals != null)
                {
                    foreach (var (start, end) in intervals)
                    {
                        pauseIntervals.Add((
                            DateTimeOffset.FromUnixTimeMilliseconds(start),
                            end.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(end.Value) : null
                        ));
                    }
                }
            }
            catch
            {
                // Ignore parsing errors, start with empty pause intervals
            }
        }

        return new Models.WorkoutSession(
            activeSessionId,
            activityType,
            string.IsNullOrEmpty(title) ? null : title,
            dataOrigin,
            DateTimeOffset.FromUnixTimeMilliseconds(startTimeMs),
            state,
            pauseIntervals
        );
    }

    /// <summary>
    /// Saves the active workout session object to preferences (for app restart persistence)
    /// </summary>
    protected void SaveWorkoutSessionToPreferences(Models.WorkoutSession session)
    {
        Preferences.Default.Set(SessionPreferenceKeys.ActiveSessionId, session.Id);
        Preferences.Default.Set(SessionPreferenceKeys.ActivityType, session.ActivityType.ToString());
        Preferences.Default.Set(SessionPreferenceKeys.Title, session.Title ?? "");
        Preferences.Default.Set(SessionPreferenceKeys.StartTime, session.StartTime.ToUnixTimeMilliseconds());
        Preferences.Default.Set(SessionPreferenceKeys.DataOrigin, session.DataOrigin);
        Preferences.Default.Set(SessionPreferenceKeys.State, session.State.ToString());

        // Serialize pause intervals to JSON
        var intervals = session.PauseIntervals
            .Select(i => (i.PauseStart.ToUnixTimeMilliseconds(), i.PauseEnd?.ToUnixTimeMilliseconds()))
            .ToList();
        var json = System.Text.Json.JsonSerializer.Serialize(intervals);
        Preferences.Default.Set(SessionPreferenceKeys.PauseIntervals, json);
    }

    /// <summary>
    /// Clears all session-related preferences
    /// </summary>
    protected void ClearSessionPreferences()
    {
        Preferences.Default.Remove(SessionPreferenceKeys.ActiveSessionId);
        Preferences.Default.Remove(SessionPreferenceKeys.ActivityType);
        Preferences.Default.Remove(SessionPreferenceKeys.Title);
        Preferences.Default.Remove(SessionPreferenceKeys.StartTime);
        Preferences.Default.Remove(SessionPreferenceKeys.DataOrigin);
        Preferences.Default.Remove(SessionPreferenceKeys.State);
        Preferences.Default.Remove(SessionPreferenceKeys.PauseIntervals);
    }
}
