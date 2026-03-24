using Maui.Health.Enums;

namespace Maui.Health.Models;

/// <summary>
/// Represents an aggregated health metric value over a time range.
/// Unlike individual records, this is a computed result (sum, average, min, max)
/// and does not have a platform record ID or data origin.
/// </summary>
public class AggregatedResult
{
    /// <summary>
    /// Start time of the aggregation period.
    /// </summary>
    public required DateTimeOffset StartTime { get; init; }

    /// <summary>
    /// End time of the aggregation period.
    /// </summary>
    public required DateTimeOffset EndTime { get; init; }

    /// <summary>
    /// The aggregated numeric value (e.g., total steps, average weight, total calories).
    /// </summary>
    public required double Value { get; init; }

    /// <summary>
    /// Unit of measurement (e.g., "kcal", "kg", "count").
    /// Null for unitless metrics like step count.
    /// </summary>
    public string? Unit { get; init; }

    /// <summary>
    /// The health data type this aggregation represents.
    /// </summary>
    public required HealthDataType DataType { get; init; }
}
