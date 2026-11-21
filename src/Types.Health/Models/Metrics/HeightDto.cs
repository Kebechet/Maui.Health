namespace Maui.Health.Models.Metrics;

/// <summary>
/// Represents height/body height data from health platforms taken at a specific point in time
/// </summary>
public class HeightDto : HealthMetricBase
{
    /// <summary>
    /// Height value
    /// </summary>
    public required double Value { get; init; }
    
    /// <summary>
    /// Unit of measurement (cm, in, m, etc.)
    /// </summary>
    public required string Unit { get; init; }
}