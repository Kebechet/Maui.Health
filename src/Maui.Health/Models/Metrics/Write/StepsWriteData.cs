namespace Maui.Health.Models.Metrics.Write;

/// <summary>
/// Write DTO for step count data over a time range
/// </summary>
public class StepsWriteData : HealthWriteRangeData
{
    /// <summary>
    /// Number of steps taken during the time period
    /// </summary>
    public required long Count { get; init; }
}
