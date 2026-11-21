using Maui.Health.Enums;

namespace Maui.Health.Models.Metrics.Write;

/// <summary>
/// Write DTO for active calories burned over a time range
/// </summary>
public class ActiveCaloriesBurnedWriteData : HealthWriteRangeData
{
    /// <summary>
    /// Energy burned in the specified <see cref="Unit"/>
    /// </summary>
    public required double Energy { get; init; }

    /// <summary>
    /// Unit of measurement
    /// </summary>
    public required EnergyUnit Unit { get; init; }
}
