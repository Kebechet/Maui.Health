namespace Maui.Health.Models.Metrics.Write;

/// <summary>
/// Write DTO for VO2 Max data (ml/kg/min)
/// </summary>
public class Vo2MaxWriteData : HealthWriteData
{
    /// <summary>
    /// VO2 Max value in ml/kg/min
    /// </summary>
    public required double Value { get; init; }
}
