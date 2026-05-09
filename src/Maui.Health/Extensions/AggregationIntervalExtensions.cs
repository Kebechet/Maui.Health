namespace Maui.Health.Extensions;

/// <summary>
/// Classifies an aggregation <see cref="TimeSpan"/> for the platform aggregator dispatch.
/// Whole-day multiples (1d, 7d, 30d, …) qualify for the calendar-aware path
/// (Health Connect's <c>aggregateGroupByPeriod</c> with <c>java.time.Period</c>;
/// HealthKit's <c>NSDateComponents { Day = N }</c>) which respects DST. Sub-day or
/// non-day-multiple intervals fall back to the fixed-duration path
/// (<c>aggregateGroupByDuration</c> / <c>NSDateComponents { Hour = …, Minute = … }</c>).
/// </summary>
public static class AggregationIntervalExtensions
{
    /// <summary>
    /// Returns <c>true</c> and the day count via <paramref name="days"/> when
    /// <paramref name="interval"/> is a positive whole-day multiple (e.g. 1d, 2d, 7d, 24h, 48h).
    /// Returns <c>false</c> for sub-day intervals, intervals with non-zero hours/minutes/seconds
    /// after the day component, zero, and negative durations.
    /// </summary>
    public static bool TryGetWholeDayCount(this TimeSpan interval, out int days)
    {
        days = 0;
        if(interval.Ticks <= 0)
        {
            return false;
        }
        // Equality against TimeSpan.FromDays(N) catches every sub-day component in one shot —
        // hours, minutes, seconds, ms, microseconds, ticks. Cleaner than checking each separately.
        if(interval != TimeSpan.FromDays(interval.Days))
        {
            return false;
        }
        days = interval.Days;
        return true;
    }
}
