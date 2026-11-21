namespace Maui.Health.Models.Metrics;

/// <summary>
/// Represents step count data from health platforms collected over a time period
/// </summary>
public class StepsDto : HealthMetricBase, IHealthTimeRange
{
    /// <summary>
    /// Number of steps taken during the time period
    /// </summary>
    public required long Count { get; init; }
    
    /// <summary>
    /// Start time of the step counting period
    /// </summary>
    public required DateTimeOffset StartTime { get; init; }
    
    /// <summary>
    /// End time of the step counting period
    /// </summary>
    public required DateTimeOffset EndTime { get; init; }
}