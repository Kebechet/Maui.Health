using Maui.Health.Enums;

namespace Maui.Health.Models.Metrics.Write;

/// <summary>
/// Write DTO for weight/body mass data
/// </summary>
public class WeightWriteData : HealthWriteData
{
    /// <summary>
    /// Weight value in the specified <see cref="Unit"/>
    /// </summary>
    public required double Value { get; init; }

    /// <summary>
    /// Unit of measurement
    /// </summary>
    public required MassUnit Unit { get; init; }
}
