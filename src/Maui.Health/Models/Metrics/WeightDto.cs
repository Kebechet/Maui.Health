namespace Maui.Health.Models.Metrics;

/// <summary>
/// Represents weight/body mass data from health platforms taken at a specific point in time
/// </summary>
public class WeightDto : HealthMetricBase
{
    /// <summary>
    /// Weight value
    /// </summary>
    public required double Value { get; init; }

    /// <summary>
    /// Unit of measurement (kg, lbs, etc.)
    /// </summary>
    public required string Unit { get; init; }
}