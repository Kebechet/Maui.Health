namespace Maui.Health.Models.Metrics;

/// <summary>
/// Represents VO2 Max (maximum oxygen consumption) data from health platforms
/// </summary>
public class Vo2MaxDto : HealthMetricBase
{
    /// <summary>
    /// VO2 Max value in milliliters per kilogram per minute (ml/kg/min)
    /// </summary>
    public required double Value { get; init; }

    /// <summary>
    /// Unit of measurement (typically "ml/kg/min")
    /// </summary>
    public required string Unit { get; init; }
}
