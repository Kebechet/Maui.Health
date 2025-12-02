using Maui.Health.Models.Metrics;

namespace Maui.Health.Models;

/// <summary>
/// Represents a group of workouts that are potential duplicates
/// (same activity type, overlapping times, different sources)
/// </summary>
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
