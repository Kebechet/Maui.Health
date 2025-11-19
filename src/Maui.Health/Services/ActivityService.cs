using Maui.Health.Constants;
using Maui.Health.Enums;
using Maui.Health.Models.Metrics;

namespace Maui.Health.Services;

public partial class ActivityService
{
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

    public partial Task End();

    /// <summary>
    /// Loads the active workout session from preferences (used for app restarts)
    /// </summary>
    protected WorkoutDto? LoadActiveWorkoutFromPreferences()
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

        if (!Enum.TryParse<ActivityType>(activityTypeStr, out var activityType) || startTimeMs <= 0)
        {
            return null;
        }

        return new WorkoutDto
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

    /// <summary>
    /// Saves the active workout session to preferences (for app restart persistence)
    /// </summary>
    protected void SaveActiveWorkoutToPreferences(WorkoutDto workoutDto)
    {
        Preferences.Default.Set(SessionPreferenceKeys.ActiveSessionId, workoutDto.Id);
        Preferences.Default.Set(SessionPreferenceKeys.ActivityType, workoutDto.ActivityType.ToString());
        Preferences.Default.Set(SessionPreferenceKeys.Title, workoutDto.Title ?? "");
        Preferences.Default.Set(SessionPreferenceKeys.StartTime, workoutDto.StartTime.ToUnixTimeMilliseconds());
        Preferences.Default.Set(SessionPreferenceKeys.DataOrigin, workoutDto.DataOrigin);
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
    }
}
