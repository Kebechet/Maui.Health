using Maui.Health.Constants;
using Maui.Health.Enums;
using Maui.Health.Extensions;
using Maui.Health.Models;
using Maui.Health.Models.Metrics;

namespace Maui.Health.Services;

/// <inheritdoc/>
public partial class HealthWorkoutService : IHealthWorkoutService
{
    /// <inheritdoc/>
    public partial Task<List<WorkoutDto>> Read(HealthTimeRange activityTime);

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public partial Task<bool> IsRunning();

    /// <inheritdoc/>
    public partial Task Start(ActivityType activityType, string? title = null);

    /// <inheritdoc/>
    public partial Task Pause();

    /// <inheritdoc/>
    public partial Task Resume();

    /// <inheritdoc/>
    public partial Task<bool> IsPaused();

    /// <summary>
    /// Ends the active workout session and returns the completed workout DTO.
    /// The caller is responsible for writing the workout to the health store.
    /// </summary>
    /// <returns>The completed WorkoutDto, or null if no active session</returns>
    public partial Task<WorkoutDto?> End();

    /// <inheritdoc/>
    public List<DuplicateWorkoutGroup> FindDuplicates(
        List<WorkoutDto> workouts,
        string dataOrigin,
        int timeThresholdMinutes = Defaults.DuplicateThresholdMinutes,
        ActivityType? activityType = null)
    {
        var duplicateGroups = new List<DuplicateWorkoutGroup>();
        var processed = new HashSet<string>();

        // Filter workouts by activity type if specified
        var workoutsToCheck = activityType is not null
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
                DataOrigin = dataOrigin,
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
    private static bool AreWorkoutsDuplicates(WorkoutDto workout1, WorkoutDto workout2, int timeThresholdMinutes)
    {
        // Must be same activity type
        if(workout1.ActivityType != workout2.ActivityType)
        {
            return false;
        }

        // Must be from different sources. Null origins are treated as "unknown",
        // two unknowns are not a known match.
        if (workout1.DataOrigin is null || workout2.DataOrigin is null)
        {
            return false;
        }
        if (workout1.DataOrigin == workout2.DataOrigin)
        {
            return false;
        }

        // Check if start times are within threshold
        var startDiff = Math.Abs((workout1.StartTime - workout2.StartTime).TotalMinutes);
        if(startDiff > timeThresholdMinutes)
        {
            return false;
        }

        // If both have end times, check those too
        if (workout1.EndTime is not null && workout2.EndTime is not null)
        {
            var endDiff = Math.Abs((workout1.EndTime.Value - workout2.EndTime.Value).TotalMinutes);
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
        var activeSessionId = Preferences.Default.Get(ActiveSessionStorage.ActiveSessionId, string.Empty);
        if (string.IsNullOrEmpty(activeSessionId))
        {
            return null;
        }

        var activityTypeStr = Preferences.Default.Get(ActiveSessionStorage.ActivityType, string.Empty);
        var title = Preferences.Default.Get(ActiveSessionStorage.Title, string.Empty);
        var startTimeMs = Preferences.Default.Get(ActiveSessionStorage.StartTime, Defaults.DefaultTimestampValue);
        var dataOriginStored = Preferences.Default.Get(ActiveSessionStorage.DataOrigin, string.Empty);
        var dataOrigin = string.IsNullOrEmpty(dataOriginStored) ? null : dataOriginStored;
        var stateStr = Preferences.Default.Get(ActiveSessionStorage.State, string.Empty);
        var pauseIntervalsJson = Preferences.Default.Get(ActiveSessionStorage.PauseIntervals, string.Empty);

        if (!Enum.TryParse<ActivityType>(activityTypeStr, out var activityType) || startTimeMs <= 0)
        {
            return null;
        }

        if (!Enum.TryParse<WorkoutSessionState>(stateStr, out var state))
        {
            state = WorkoutSessionState.Running;
        }

        // Parse pause intervals from JSON
        var pauseIntervals = pauseIntervalsJson.ToDateRanges();

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
        Preferences.Default.Set(ActiveSessionStorage.ActiveSessionId, session.Id);
        Preferences.Default.Set(ActiveSessionStorage.ActivityType, session.ActivityType.ToString());
        Preferences.Default.Set(ActiveSessionStorage.Title, session.Title ?? "");
        Preferences.Default.Set(ActiveSessionStorage.StartTime, session.StartTime.ToUnixTimeMilliseconds());
        Preferences.Default.Set(ActiveSessionStorage.DataOrigin, session.DataOrigin ?? string.Empty);
        Preferences.Default.Set(ActiveSessionStorage.State, session.State.ToString());

        var json = session.PauseIntervals.ToJson();
        Preferences.Default.Set(ActiveSessionStorage.PauseIntervals, json);
    }

    /// <summary>
    /// Clears all session-related preferences
    /// </summary>
    protected void ClearSessionPreferences()
    {
        Preferences.Default.Remove(ActiveSessionStorage.ActiveSessionId);
        Preferences.Default.Remove(ActiveSessionStorage.ActivityType);
        Preferences.Default.Remove(ActiveSessionStorage.Title);
        Preferences.Default.Remove(ActiveSessionStorage.StartTime);
        Preferences.Default.Remove(ActiveSessionStorage.DataOrigin);
        Preferences.Default.Remove(ActiveSessionStorage.State);
        Preferences.Default.Remove(ActiveSessionStorage.PauseIntervals);
    }

}
