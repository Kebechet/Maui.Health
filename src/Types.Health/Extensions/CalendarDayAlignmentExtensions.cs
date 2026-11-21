namespace Maui.Health.Extensions;

/// <summary>
/// Snaps a moment to the start of its calendar day in a given time zone, returning a
/// <see cref="DateTimeOffset"/> at local midnight with the zone's offset at that midnight.
/// Used by the calendar-day aggregator to anchor 1-day buckets to local-day boundaries
/// instead of "windowStart + N×1day" slots that drift with the sync trigger time.
/// </summary>
public static class CalendarDayAlignmentExtensions
{
    /// <summary>
    /// Returns the moment at 00:00 of <paramref name="moment"/>'s calendar day in
    /// <paramref name="timeZone"/>, expressed as a <see cref="DateTimeOffset"/> with the
    /// zone's offset at that midnight (which can differ from <paramref name="moment"/>'s
    /// own offset across DST transitions — e.g. on the spring-forward day midnight is still
    /// the pre-transition offset because the jump happens at 02:00).
    /// </summary>
    public static DateTimeOffset SnapToCalendarDayStart(this DateTimeOffset moment, TimeZoneInfo timeZone)
    {
        var localInZone = TimeZoneInfo.ConvertTime(moment, timeZone);
        var localMidnight = localInZone.Date;
        var offsetAtMidnight = timeZone.GetUtcOffset(localMidnight);
        return new DateTimeOffset(localMidnight, offsetAtMidnight);
    }
}
