namespace Maui.Health.Models.Metrics.Write;

/// <summary>
/// Write DTO for blood pressure data (mmHg)
/// </summary>
public class BloodPressureWriteData : HealthWriteData
{
    /// <summary>
    /// Systolic blood pressure in mmHg
    /// </summary>
    public required double Systolic { get; init; }

    /// <summary>
    /// Diastolic blood pressure in mmHg
    /// </summary>
    public required double Diastolic { get; init; }
}
