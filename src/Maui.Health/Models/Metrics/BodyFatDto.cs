namespace Maui.Health.Models.Metrics;

/// <summary>
/// Represents body fat percentage data from health platforms
/// </summary>
public class BodyFatDto : HealthMetricBase
{
    /// <summary>
    /// Body fat percentage (0-100)
    /// </summary>
    public required double Percentage { get; init; }

    /// <summary>
    /// Unit of measurement (typically "%")
    /// </summary>
    public required string Unit { get; init; }
}
