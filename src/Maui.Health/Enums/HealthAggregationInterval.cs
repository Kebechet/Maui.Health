namespace Maui.Health.Enums;

/// <summary>
/// Preset aggregation intervals for bucketed health queries.
/// </summary>
public enum HealthAggregationInterval
{
    /// <summary>
    /// Aggregate in one-minute buckets.
    /// </summary>
    Minute = 1,

    /// <summary>
    /// Aggregate in one-hour buckets.
    /// </summary>
    Hour = 2,

    /// <summary>
    /// Aggregate in one-day buckets.
    /// </summary>
    Day = 3
}
