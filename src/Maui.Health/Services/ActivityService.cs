using Maui.Health.Models.Metrics;

namespace Maui.Health.Services;

public partial class ActivityService
{
    public partial Task<List<WorkoutDto>> Read(HealthTimeRange activityTime);

    public partial Task<WorkoutDto> ReadActive(HealthTimeRange activityTime);

    public partial Task Write(WorkoutDto workout);

    public partial Task Delete(WorkoutDto workout);

    public partial Task<bool> IsSessionRunning();

    public partial Task StartNewSession(WorkoutDto workoutDto);

    public partial Task EndActiveSession();
}
