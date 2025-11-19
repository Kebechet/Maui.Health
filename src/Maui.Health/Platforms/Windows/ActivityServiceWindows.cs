using Maui.Health.Models.Metrics;

namespace Maui.Health.Services;

public partial class ActivityService
{
    public partial Task<List<WorkoutDto>> Read(HealthTimeRange activityTime)
    {
        try
        {
            return Task.FromResult(new List<WorkoutDto>());
        }
        catch (Exception)
        {
            return Task.FromResult(new List<WorkoutDto>());
        }
    }

    public partial Task<WorkoutDto> GetActive(HealthTimeRange activityTime)
    {
        try
        {
            return Task.FromResult<WorkoutDto>(null!);
        }
        catch (Exception)
        {
            return Task.FromResult<WorkoutDto>(null!);
        }
    }

    public partial Task Write(WorkoutDto workout)
    {
        try
        {
            return Task.CompletedTask;
        }
        catch (Exception)
        {
            return Task.CompletedTask;
        }
    }

    public partial Task Delete(WorkoutDto workout)
    {
        try
        {
            return Task.CompletedTask;
        }
        catch (Exception)
        {
            return Task.CompletedTask;
        }
    }

    public partial Task<bool> IsRunning()
    {
        try
        {
            return Task.FromResult(false);
        }
        catch (Exception)
        {
            return Task.FromResult(false);
        }
    }

    public partial Task Start(WorkoutDto workoutDto)
    {
        try
        {
            return Task.CompletedTask;
        }
        catch (Exception)
        {
            return Task.CompletedTask;
        }
    }

    public partial Task Pause()
    {
        try
        {
            return Task.CompletedTask;
        }
        catch (Exception)
        {
            return Task.CompletedTask;
        }
    }

    public partial Task Resume()
    {
        try
        {
            return Task.CompletedTask;
        }
        catch (Exception)
        {
            return Task.CompletedTask;
        }
    }

    public partial Task<bool> IsPaused()
    {
        try
        {
            return Task.FromResult(false);
        }
        catch (Exception)
        {
            return Task.FromResult(false);
        }
    }

    public partial Task End()
    {
        try
        {
            return Task.CompletedTask;
        }
        catch (Exception)
        {
            return Task.CompletedTask;
        }
    }
}
