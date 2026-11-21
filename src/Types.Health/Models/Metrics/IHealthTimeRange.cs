namespace Maui.Health.Models.Metrics;

/// <summary>
/// Interface for health metrics that represent data collected over a time range/duration
/// </summary>
public interface IHealthTimeRange
{
    /// <summary>
    /// Start time of the measurement period
    /// </summary>
    public DateTimeOffset StartTime { get; }

    /// <summary>
    /// End time of the measurement period
    /// </summary>
    public DateTimeOffset EndTime { get; }

    /// <summary>
    /// Duration of the measurement period
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;
}