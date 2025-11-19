using Maui.Health.Enums;

namespace Maui.Health.Models;

/// <summary>
/// Tracks a workout session including pause/resume functionality
/// </summary>
public class WorkoutSession
{
    /// <summary>
    /// Unique identifier for the session (UUID)
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Type of activity being performed
    /// </summary>
    public ActivityType ActivityType { get; }

    /// <summary>
    /// Title of the workout session
    /// </summary>
    public string? Title { get; }

    /// <summary>
    /// Data origin identifier
    /// </summary>
    public string DataOrigin { get; }

    /// <summary>
    /// When the session started
    /// </summary>
    public DateTimeOffset StartTime { get; }

    /// <summary>
    /// Current state of the session
    /// </summary>
    public WorkoutSessionState State { get; private set; }

    /// <summary>
    /// List of pause intervals (start and end times)
    /// </summary>
    public List<(DateTimeOffset PauseStart, DateTimeOffset? PauseEnd)> PauseIntervals { get; }

    /// <summary>
    /// Gets the total paused duration in seconds
    /// </summary>
    public double TotalPausedSeconds
    {
        get
        {
            var total = 0.0;
            foreach (var (pauseStart, pauseEnd) in PauseIntervals)
            {
                var end = pauseEnd ?? DateTimeOffset.UtcNow;
                total += (end - pauseStart).TotalSeconds;
            }
            return total;
        }
    }

    /// <summary>
    /// Gets the active duration (excluding paused time) in seconds
    /// </summary>
    public double ActiveDurationSeconds
    {
        get
        {
            var totalElapsed = (DateTimeOffset.UtcNow - StartTime).TotalSeconds;
            return totalElapsed - TotalPausedSeconds;
        }
    }

    public WorkoutSession(
        string id,
        ActivityType activityType,
        string? title,
        string dataOrigin,
        DateTimeOffset startTime,
        WorkoutSessionState state = WorkoutSessionState.Running,
        List<(DateTimeOffset, DateTimeOffset?)>? pauseIntervals = null)
    {
        Id = id;
        ActivityType = activityType;
        Title = title;
        DataOrigin = dataOrigin;
        StartTime = startTime;
        State = state;
        PauseIntervals = pauseIntervals ?? new List<(DateTimeOffset, DateTimeOffset?)>();
    }

    public void Pause()
    {
        if (State != WorkoutSessionState.Running)
        {
            throw new InvalidOperationException($"Cannot pause session in state: {State}");
        }

        State = WorkoutSessionState.Paused;
        PauseIntervals.Add((DateTimeOffset.UtcNow, null));
    }

    public void Resume()
    {
        if (State != WorkoutSessionState.Paused)
        {
            throw new InvalidOperationException($"Cannot resume session in state: {State}");
        }

        // Close the current pause interval
        if (PauseIntervals.Count > 0)
        {
            var lastPause = PauseIntervals[^1];
            if (lastPause.Item2 == null)
            {
                PauseIntervals[^1] = (lastPause.Item1, DateTimeOffset.UtcNow);
            }
        }

        State = WorkoutSessionState.Running;
    }

    public void End()
    {
        // If we're paused when ending, close the pause interval
        if (State == WorkoutSessionState.Paused && PauseIntervals.Count > 0)
        {
            var lastPause = PauseIntervals[^1];
            if (lastPause.Item2 == null)
            {
                PauseIntervals[^1] = (lastPause.Item1, DateTimeOffset.UtcNow);
            }
        }

        State = WorkoutSessionState.Ended;
    }
}
