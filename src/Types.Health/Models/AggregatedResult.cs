using Maui.Health.Enums;

namespace Maui.Health.Models;

/// <summary>
/// Represents an aggregated health metric value over a time range.
/// Unlike individual records, this is a computed result (sum, average, min, max)
/// and does not have a platform record ID.
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

    /// <summary>
    /// The primary health platform that produced this aggregated result.
    /// On Android: <see cref="HealthDataSdk.GoogleHealthConnect"/>.
    /// On iOS: <see cref="HealthDataSdk.AppleHealthKit"/>.
    /// </summary>
    public required HealthDataSdk DataSdk { get; init; }

    /// <summary>
    /// Stable identifiers of the apps that contributed to this aggregated value.
    /// iOS: <c>HKSource.BundleIdentifier</c> values.
    /// Android: Health Connect <c>DataOrigin.PackageName</c> values.
    /// Multiple entries are possible because aggregation combines records from all health apps.
    /// </summary>
    public List<string> DataOrigins { get; init; } = [];
}
