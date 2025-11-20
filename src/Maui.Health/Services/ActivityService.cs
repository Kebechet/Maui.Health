using Maui.Health.Constants;
using Maui.Health.Enums;
using Maui.Health.Models;
using Maui.Health.Models.Metrics;
using static Maui.Health.Constants.HealthConstants;

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

    /// <summary>
    /// Gets the currently active workout session, if any.
    /// </summary>
    /// <returns>The active WorkoutSession, or null if no session is running</returns>
    public partial Task<WorkoutSession?> GetActive();

    public partial Task<bool> IsRunning();

    /// <summary>
    /// Starts a new workout session with the specified activity type
    /// </summary>
    /// <param name="activityType">The type of activity being performed</param>
    /// <param name="title">Optional title for the workout</param>
    /// <param name="dataOrigin">Optional data origin identifier (defaults to app package name)</param>
    public partial Task Start(ActivityType activityType, string? title = null, string? dataOrigin = null);

    public partial Task Pause();

    public partial Task Resume();

    public partial Task<bool> IsPaused();

    /// <summary>
    /// Ends the active workout session and returns the completed workout DTO.
    /// The caller is responsible for writing the workout to the health store.
    /// </summary>
    /// <returns>The completed WorkoutDto, or null if no active session</returns>
    public partial Task<WorkoutDto?> End();

    /// <summary>
    /// Finds duplicate workout groups from a list of workouts.
    /// Duplicates are identified by: same activity type, different sources, and overlapping times.
    /// </summary>
    /// <param name="workouts">List of workouts to check for duplicates</param>
    /// <param name="appSource">Your app's data origin identifier (e.g., "DemoApp")</param>
    /// <param name="timeThresholdMinutes">Maximum time difference in minutes to consider as duplicate (default: 5)</param>
    /// <param name="activityType">Optional activity type filter - only find duplicates of this type</param>
    /// <returns>List of duplicate groups, each containing matching workouts</returns>
    public List<DuplicateWorkoutGroup> FindDuplicates(
        List<WorkoutDto> workouts,
        string appSource,
        int timeThresholdMinutes = Defaults.DuplicateThresholdMinutes,
        ActivityType? activityType = null)
    {
        var duplicateGroups = new List<DuplicateWorkoutGroup>();
        var processed = new HashSet<string>();

        // Filter workouts by activity type if specified
        var workoutsToCheck = activityType.HasValue
            ? workouts.Where(w => w.ActivityType == activityType.Value).ToList()
            : workouts;

        foreach (var workout in workoutsToCheck)
        {
            if (processed.Contains(workout.Id))
            {
                continue;
            }

            // Find all workouts that match this one
            var matches = workoutsToCheck.Where(w =>
                w.Id != workout.Id &&
                !processed.Contains(w.Id) &&
                AreWorkoutsDuplicates(workout, w, timeThresholdMinutes)
            ).ToList();

            if (!matches.Any())
            {
                continue;
            }

            // Create a group with the original workout and all matches
            var group = new DuplicateWorkoutGroup
            {
                AppSource = appSource,
                Workouts = [workout, .. matches]
            };

            duplicateGroups.Add(group);

            // Mark all as processed
            processed.Add(workout.Id);
            foreach (var match in matches)
            {
                processed.Add(match.Id);
            }
        }

        return duplicateGroups;
    }

    /// <summary>
    /// Checks if two workouts are likely duplicates based on activity type, source, and time overlap.
    /// </summary>
    private static bool AreWorkoutsDuplicates(WorkoutDto w1, WorkoutDto w2, int timeThresholdMinutes)
    {
        // Must be same activity type
        if(w1.ActivityType != w2.ActivityType)
        {
            return false;
        }

        // Must be from different sources
        if(w1.DataOrigin == w2.DataOrigin)
        {
            return false;
        }

        // Check if start times are within threshold
        var startDiff = Math.Abs((w1.StartTime - w2.StartTime).TotalMinutes);
        if(startDiff > timeThresholdMinutes)
        {
            return false;
        }

        // If both have end times, check those too
        if (w1.EndTime.HasValue && w2.EndTime.HasValue)
        {
            var endDiff = Math.Abs((w1.EndTime.Value - w2.EndTime.Value).TotalMinutes);
            if (endDiff > timeThresholdMinutes)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Loads the active workout session object from preferences (used for app restarts)
    /// </summary>
    protected WorkoutSession? LoadWorkoutSessionFromPreferences()
    {
        var activeSessionId = Preferences.Default.Get(SessionPreferenceKeys.ActiveSessionId, string.Empty);
        if (string.IsNullOrEmpty(activeSessionId))
        {
            return null;
        }

        var activityTypeStr = Preferences.Default.Get(SessionPreferenceKeys.ActivityType, string.Empty);
        var title = Preferences.Default.Get(SessionPreferenceKeys.Title, string.Empty);
        var startTimeMs = Preferences.Default.Get(SessionPreferenceKeys.StartTime, Defaults.DefaultTimestampValue);
        var dataOrigin = Preferences.Default.Get(SessionPreferenceKeys.DataOrigin, string.Empty);
        var stateStr = Preferences.Default.Get(SessionPreferenceKeys.State, string.Empty);
        var pauseIntervalsJson = Preferences.Default.Get(SessionPreferenceKeys.PauseIntervals, string.Empty);

        if (!Enum.TryParse<ActivityType>(activityTypeStr, out var activityType) || startTimeMs <= 0)
        {
            return null;
        }

        if (!Enum.TryParse<WorkoutSessionState>(stateStr, out var state))
        {
            state = WorkoutSessionState.Running;
        }

        // Parse pause intervals from JSON
        var pauseIntervals = ParsePauseIntervals(pauseIntervalsJson);

        return new WorkoutSession(
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

    /// <summary>
    /// Parses pause intervals from JSON string
    /// </summary>
    private static List<(DateTimeOffset, DateTimeOffset?)> ParsePauseIntervals(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return [];
        }

        try
        {
            var intervals = System.Text.Json.JsonSerializer.Deserialize<List<(long, long?)>>(json);
            if (intervals is null)
            {
                return [];
            }

            return intervals.Select(i => (
                DateTimeOffset.FromUnixTimeMilliseconds(i.Item1),
                i.Item2.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(i.Item2.Value) : (DateTimeOffset?)null
            )).ToList();
        }
        catch
        {
            return [];
        }
    }
}
