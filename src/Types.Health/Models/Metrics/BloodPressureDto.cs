namespace Maui.Health.Models.Metrics;

/// <summary>
/// Represents blood pressure data from health platforms
/// Note: iOS stores systolic and diastolic as separate records,
/// but they are combined here for a unified API
/// </summary>
public class BloodPressureDto : HealthMetricBase
{
    /// <summary>
    /// Systolic blood pressure (top number) in mmHg
    /// </summary>
    public required double Systolic { get; init; }

    /// <summary>
    /// Diastolic blood pressure (bottom number) in mmHg
    /// </summary>
    public required double Diastolic { get; init; }

    /// <summary>
    /// Unit of measurement (typically "mmHg")
    /// </summary>
    public required string Unit { get; init; }
}
