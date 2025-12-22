using Maui.Health.Enums;
using Maui.Health.Models;
using Maui.Health.Models.Metrics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maui.Health.Services;

public partial class HealthWorkoutService
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

    public partial Task<WorkoutSession?> GetActive()
    {
        try
        {
            return Task.FromResult<WorkoutSession?>(null);
        }
        catch (Exception)
        {
            return Task.FromResult<WorkoutSession?>(null);
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

    public partial Task Start(ActivityType activityType, string? title = null, string? dataOrigin = null)
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

    public partial Task<WorkoutDto?> End()
    {
        try
        {
            return Task.FromResult<WorkoutDto?>(null);
        }
        catch (Exception)
        {
            return Task.FromResult<WorkoutDto?>(null);
        }
    }
}
