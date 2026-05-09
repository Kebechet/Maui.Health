namespace Maui.Health.Enums;

/// <summary>
/// Calendar-aware bucket unit for
/// <see cref="Services.IHealthService.GetAggregatedHealthDataByCalendarPeriod{TDto}"/>. Each
/// value maps to a Java <c>Period</c> factory (<c>ofDays</c>, <c>ofWeeks</c>, <c>ofMonths</c>,
/// <c>ofYears</c>) on Android and an <c>NSDateComponents</c> field on iOS, both of which walk
/// the system calendar and respect calendar-length variance (DST 23h/25h days, 28-31-day
/// months, 365/366-day years).
/// </summary>
/// <remarks>
/// Sub-day units like hours and minutes intentionally aren't here — calendar variance doesn't
/// apply to them. Use <see cref="Services.IHealthService.GetAggregatedHealthDataByInterval{TDto}"/>
/// with a <see cref="System.TimeSpan"/> for sub-day buckets.
/// </remarks>
public enum CalendarUnit
{
    /// <summary>One calendar day in the caller's time zone. Spans 23, 24, or 25 wall-clock hours
    /// across DST transitions.</summary>
    Day,

    /// <summary>Seven calendar days. Always exactly 7×day-length, so DST variance applies once
    /// inside any week containing a transition (the week is 167 or 169 hours long instead of 168).</summary>
    Week,

    /// <summary>One calendar month, 28-31 days depending on the month and leap year. Anchors to
    /// the day-of-month of the time-range start (e.g. range starting May 14 produces buckets
    /// May 14 → June 14, June 14 → July 14, …).</summary>
    Month,

    /// <summary>One calendar year, 365 or 366 days depending on leap year. Anchors to the
    /// month-and-day of the time-range start.</summary>
    Year,
}
