using Maui.Health.Enums;

namespace Maui.Health.Models.Metrics.Write;

/// <summary>
/// Write DTO for height data
/// </summary>
public class HeightWriteData : HealthWriteData
{
    /// <summary>
    /// Height value in the specified <see cref="Unit"/>
    /// </summary>
    public required double Value { get; init; }

    /// <summary>
    /// Unit of measurement
    /// </summary>
    public required LengthUnit Unit { get; init; }
}
