namespace Maui.Health.Models.Metrics;

/// <summary>
/// Represents active calories burned data from health platforms collected over a time period.
/// Active calories are the calories burned during physical activity and exercise.
/// </summary>
public class ActiveCaloriesBurnedDto : HealthMetricBase, IHealthTimeRange
{
    /// <summary>
    /// Amount of active energy (calories) burned during the time period
    /// </summary>
    public required double Energy { get; init; }

    /// <summary>
    /// Unit of measurement (kcal, kJ, etc.)
    /// </summary>
    public required string Unit { get; init; }

    /// <summary>
    /// Start time of the energy burning period
    /// </summary>
    public required DateTimeOffset StartTime { get; init; }

    /// <summary>
    /// End time of the energy burning period
    /// </summary>
    public required DateTimeOffset EndTime { get; init; }
}
