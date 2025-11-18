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
    public partial Task<WorkoutDto> GetActiveSession(HealthTimeRange activityTime);

    public partial Task<bool> IsSessionRunning();

    public partial Task StartNewSession(WorkoutDto workoutDto);

    public partial Task EndActiveSession();
}
