namespace Maui.Health.Models.Metrics.Write;

/// <summary>
/// Write DTO for body fat percentage data
/// </summary>
public class BodyFatWriteData : HealthWriteData
{
    /// <summary>
    /// Body fat percentage (0-100)
    /// </summary>
    public required double Percentage { get; init; }
}
