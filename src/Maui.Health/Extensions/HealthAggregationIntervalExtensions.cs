using Maui.Health.Enums;

namespace Maui.Health.Extensions;

/// <summary>
/// Conversion helpers for <see cref="HealthAggregationInterval"/>.
/// </summary>
public static class HealthAggregationIntervalExtensions
{
    /// <summary>
    /// Converts an aggregation interval preset to a <see cref="TimeSpan"/>.
    /// </summary>
    public static TimeSpan ToTimeSpan(this HealthAggregationInterval interval)
    {
        return interval switch
        {
            HealthAggregationInterval.Minute => TimeSpan.FromMinutes(1),
            HealthAggregationInterval.Hour => TimeSpan.FromHours(1),
            HealthAggregationInterval.Day => TimeSpan.FromDays(1),
            _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, "Unsupported aggregation interval."),
        };
    }
}
