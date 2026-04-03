using Maui.Health.Models.Metrics;

namespace Maui.Health.Extensions;

/// <summary>
/// Extension methods for <see cref="DateOnly"/> to create <see cref="HealthTimeRange"/> instances.
/// </summary>
public static class DateOnlyExtensions
{
    /// <summary>
    /// Creates a <see cref="HealthTimeRange"/> spanning the full day for the given date.
    /// For today's date, the range ends at <see cref="DateTime.Now"/> instead of end of day.
    /// </summary>
    /// <param name="date">The date to create a full-day range for</param>
    /// <returns>A <see cref="HealthTimeRange"/> from start of day to end of day (or now if today)</returns>
    public static HealthTimeRange ToFullDayHealthTimeRange(this DateOnly date)
    {
        var start = date.ToDateTime(TimeOnly.MinValue);
        var end = date == DateOnly.FromDateTime(DateTime.Today)
            ? DateTime.Now
            : date.ToDateTime(TimeOnly.MaxValue);

        return HealthTimeRange.FromDateTime(start, end);
    }
}
