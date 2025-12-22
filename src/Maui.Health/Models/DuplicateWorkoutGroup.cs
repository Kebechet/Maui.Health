using Maui.Health.Models.Metrics;

namespace Maui.Health.Models;

/// <summary>
/// Represents a group of workouts that are potential duplicates
/// (same activity type, overlapping times, different sources).
/// </summary>
/// <remarks>
/// <para>
/// This class is used to detect and merge duplicate workout sessions that occur when a user
/// stops a workout on both their phone and watch (or other wearable device) simultaneously.
/// </para>
/// <para>
/// <b>Usage scenario:</b> When a user ends a workout session, you can query health data with a
/// configurable time threshold (e.g., 10 minutes) to detect if multiple workout records exist
/// from different data sources (your app vs. watch/wearable) for the same exercise session.
/// </para>
/// <para>
/// <b>Merge strategy:</b> When duplicates are detected, the recommended approach is to:
/// <list type="number">
///   <item>Keep the external workout (from watch/wearable) as it typically contains richer health data (heart rate, GPS, etc.)</item>
///   <item>Delete your app's workout record to avoid duplication</item>
///   <item>Optionally mark/tag the external workout to indicate it was associated with your app's session</item>
/// </list>
/// </para>
/// </remarks>
public class DuplicateWorkoutGroup
{
    /// <summary>
    /// All workouts in this duplicate group
    /// </summary>
    public List<WorkoutDto> Workouts { get; set; } = [];

    /// <summary>
    /// The app source identifier used to identify which workout came from your app
    /// </summary>
    public string AppSource { get; set; } = string.Empty;

    /// <summary>
    /// The workout from your app (matching AppSource)
    /// </summary>
    public WorkoutDto? AppWorkout => Workouts.FirstOrDefault(w => w.DataOrigin == AppSource);

    /// <summary>
    /// The workout from external source (watch, other app)
    /// </summary>
    public WorkoutDto? ExternalWorkout => Workouts.FirstOrDefault(w => w.DataOrigin != AppSource);

    /// <summary>
    /// All external workouts (if more than one)
    /// </summary>
    public List<WorkoutDto> ExternalWorkouts => Workouts.Where(w => w.DataOrigin != AppSource).ToList();

    /// <summary>
    /// Time difference between start times in minutes
    /// </summary>
    public double StartTimeDifferenceMinutes
    {
        get
        {
            if (AppWorkout == null || ExternalWorkout == null)
            {
                return 0;
            }

            return Math.Abs((AppWorkout.StartTime - ExternalWorkout.StartTime).TotalMinutes);
        }
    }

    /// <summary>
    /// Time difference between end times in minutes (if both have end times)
    /// </summary>
    public double? EndTimeDifferenceMinutes
    {
        get
        {
            if (AppWorkout?.EndTime == null || ExternalWorkout?.EndTime == null)
            {
                return null;
            }

            return Math.Abs((AppWorkout.EndTime.Value - ExternalWorkout.EndTime.Value).TotalMinutes);
        }
    }
}
