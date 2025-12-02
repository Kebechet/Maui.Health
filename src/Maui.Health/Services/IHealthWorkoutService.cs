using Maui.Health.Enums;
using Maui.Health.Models;
using Maui.Health.Models.Metrics;

namespace Maui.Health.Services;

/// <summary>
/// Interface for workout/activity tracking operations
/// </summary>
public interface IHealthWorkoutService
{
    /// <summary>
    /// Reads workout records from the health data store within the specified time range
    /// </summary>
    /// <param name="activityTime">The time range for data retrieval</param>
    /// <returns>List of workout DTOs</returns>
    Task<List<WorkoutDto>> Read(HealthTimeRange activityTime);

    /// <summary>
    /// Writes a workout record to the health data store
    /// </summary>
    /// <param name="workout">The workout record to write</param>
    Task Write(WorkoutDto workout);

    /// <summary>
    /// Deletes a workout record from the health data store
    /// </summary>
    /// <param name="workout">The workout record to delete</param>
    /// <remarks>
    /// Note: You can only delete workout records that were created by your application.
    /// Attempting to delete workouts created by other apps will fail or be ignored by the platform.
    /// </remarks>
    Task Delete(WorkoutDto workout);

    /// <summary>
    /// Gets the currently active workout session, if any
    /// </summary>
    /// <returns>The active WorkoutSession, or null if no session is running</returns>
    Task<WorkoutSession?> GetActive();

    /// <summary>
    /// Checks if a workout session is currently running
    /// </summary>
    /// <returns>True if a session is running, false otherwise</returns>
    Task<bool> IsRunning();

    /// <summary>
    /// Starts a new workout session with the specified activity type
    /// </summary>
    /// <param name="activityType">The type of activity being performed</param>
    /// <param name="title">Optional title for the workout</param>
    /// <param name="dataOrigin">Optional data origin identifier (defaults to app package name)</param>
    Task Start(ActivityType activityType, string? title = null, string? dataOrigin = null);

    /// <summary>
    /// Pauses the currently active workout session
    /// </summary>
    Task Pause();

    /// <summary>
    /// Resumes a paused workout session
    /// </summary>
    Task Resume();

    /// <summary>
    /// Checks if the current workout session is paused
    /// </summary>
    /// <returns>True if the session is paused, false otherwise</returns>
    Task<bool> IsPaused();

    /// <summary>
    /// Ends the active workout session and returns the completed workout DTO.
    /// The caller is responsible for writing the workout to the health store.
    /// </summary>
    /// <returns>The completed WorkoutDto, or null if no active session</returns>
    Task<WorkoutDto?> End();

    /// <summary>
    /// Finds duplicate workout groups from a list of workouts.
    /// Duplicates are identified by: same activity type, different sources, and overlapping times.
    /// </summary>
    /// <param name="workouts">List of workouts to check for duplicates</param>
    /// <param name="appSource">Your app's data origin identifier (e.g., "DemoApp")</param>
    /// <param name="timeThresholdMinutes">Maximum time difference in minutes to consider as duplicate (default: 5)</param>
    /// <param name="activityType">Optional activity type filter - only find duplicates of this type</param>
    /// <returns>List of duplicate groups, each containing matching workouts</returns>
    List<DuplicateWorkoutGroup> FindDuplicates(
        List<WorkoutDto> workouts,
        string appSource,
        int timeThresholdMinutes = 5,
        ActivityType? activityType = null);
}
