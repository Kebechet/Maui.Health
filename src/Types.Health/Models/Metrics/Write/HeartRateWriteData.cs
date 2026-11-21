namespace Maui.Health.Models.Metrics.Write;

/// <summary>
/// Write DTO for heart rate data
/// </summary>
public class HeartRateWriteData : HealthWriteData
{
    /// <summary>
    /// Heart rate in beats per minute
    /// </summary>
    public required double BeatsPerMinute { get; init; }
}
