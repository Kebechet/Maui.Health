namespace Maui.Health.Models.Metrics;

/// <summary>
/// Represents heart rate data from health platforms taken at a specific point in time
/// </summary>
public class HeartRateDto : HealthMetricBase
{
    /// <summary>
    /// Heart rate value in beats per minute (BPM)
    /// </summary>
    public required double BeatsPerMinute { get; init; }

    /// <summary>
    /// Unit of measurement (always BPM for heart rate)
    /// </summary>
    public required string Unit { get; init; }
}
