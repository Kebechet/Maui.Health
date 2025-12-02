using Maui.Health.Constants;
using Maui.Health.Enums;
using Maui.Health.Models;
using Maui.Health.Models.Metrics;

namespace Maui.Health.Extensions;

/// <summary>
/// Extension methods for converting between WorkoutSession and WorkoutDto
/// </summary>
public static class WorkoutSessionExtensions
{
    /// <summary>
    /// Converts a WorkoutSession to a WorkoutDto
    /// </summary>
    /// <param name="session">The WorkoutSession to convert</param>
    /// <param name="endTime">The end time for the workout (defaults to current time if null)</param>
    /// <param name="energyBurned">Optional energy burned in kilocalories</param>
    /// <param name="distance">Optional distance in meters</param>
    /// <param name="averageHeartRate">Optional average heart rate in BPM</param>
    /// <param name="maxHeartRate">Optional max heart rate in BPM</param>
    /// <param name="minHeartRate">Optional min heart rate in BPM</param>
    /// <returns>A WorkoutDto representing the completed workout</returns>
    public static WorkoutDto ToWorkoutDto(
        this WorkoutSession session,
        DateTimeOffset? endTime = null,
        double? energyBurned = null,
        double? distance = null,
        double? averageHeartRate = null,
        double? maxHeartRate = null,
        double? minHeartRate = null)
    {
        var actualEndTime = endTime ?? DateTimeOffset.UtcNow;

        // Calculate durations
        var totalElapsed = (actualEndTime - session.StartTime).TotalSeconds;
        var activeDuration = totalElapsed - session.TotalPausedSeconds;

        return new WorkoutDto
        {
            Id = session.Id,
            DataOrigin = session.DataOrigin,
            Timestamp = session.StartTime,
            ActivityType = session.ActivityType,
            Title = session.Title,
            StartTime = session.StartTime,
            EndTime = actualEndTime,
            EnergyBurned = energyBurned,
            Distance = distance,
            AverageHeartRate = averageHeartRate,
            MaxHeartRate = maxHeartRate,
            MinHeartRate = minHeartRate,
            // Store pause/resume metadata
            Metadata = new Dictionary<string, object>
            {
                { WorkoutMetadata.ActiveDurationSeconds, activeDuration },
                { WorkoutMetadata.PausedDurationSeconds, session.TotalPausedSeconds },
                { WorkoutMetadata.PauseCount, session.PauseIntervals.Count },
                { WorkoutMetadata.PauseIntervals, session.PauseIntervals.ToJson() }
            }
        };
    }

    /// <summary>
    /// Converts a WorkoutDto back to a WorkoutSession
    /// </summary>
    /// <param name="workout">The WorkoutDto to convert</param>
    /// <returns>A WorkoutSession if the workout has session metadata, otherwise null</returns>
    public static WorkoutSession? ToWorkoutSession(this WorkoutDto workout)
    {
        // If the workout doesn't have an end time, it might be an active session
        var state = workout.EndTime.HasValue
            ? WorkoutSessionState.Ended
            : WorkoutSessionState.Running;

        // Try to extract pause intervals from metadata
        var pauseIntervals = new List<DateRange>();
        if (workout.Metadata?.TryGetValue(WorkoutMetadata.PauseIntervals, out var intervalsObj) == true)
        {
            pauseIntervals = intervalsObj.ToDateRanges();
        }

        return new WorkoutSession(
            workout.Id,
            workout.ActivityType,
            workout.Title,
            workout.DataOrigin,
            workout.StartTime,
            state,
            pauseIntervals
        );
    }

    /// <summary>
    /// Creates a WorkoutDto from a WorkoutSession, preserving existing workout data
    /// </summary>
    /// <param name="session">The WorkoutSession to convert</param>
    /// <param name="existingWorkout">The existing WorkoutDto to preserve data from</param>
    /// <param name="endTime">Optional override for end time</param>
    /// <returns>A WorkoutDto with session data and preserved workout metrics</returns>
    public static WorkoutDto ToWorkoutDto(
        this WorkoutSession session,
        WorkoutDto existingWorkout,
        DateTimeOffset? endTime = null)
    {
        return session.ToWorkoutDto(
            endTime,
            existingWorkout.EnergyBurned,
            existingWorkout.Distance,
            existingWorkout.AverageHeartRate,
            existingWorkout.MaxHeartRate,
            existingWorkout.MinHeartRate
        );
    }
}
