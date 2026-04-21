namespace Maui.Health.Models.Metrics;

/// <summary>
/// Represents a time range for querying health data.
/// </summary>
public class HealthTimeRange : IHealthTimeRange
{
    /// <summary>
    /// Lower bound that <see cref="StartTime"/> will accept. Anything below is silently
    /// clamped up to this floor on construction.
    /// </summary>
    /// <remarks>
    /// <para><b>Why a floor exists at all.</b> Both Apple HealthKit's <c>HKStatisticsCollectionQuery</c>
    /// and Android Health Connect's <c>aggregateGroupByDuration</c> bucket their results by
    /// walking from the start of the time range to the end in fixed intervals. With an absurd
    /// anchor like <see cref="System.DateTimeOffset.MinValue"/> (year 0001) and a daily
    /// interval, the platform is asked to materialize ~739,000 buckets in a single call. In
    /// practice both aggregators silently return an empty list (or throw, which the library
    /// catches) when the range is unworkable — leaving callers debugging "why did my query
    /// return nothing?" with no signal that the input was the actual problem.</para>
    ///
    /// <para><b>Why Unix epoch (1970-01-01).</b> This is the earliest point at which both
    /// <c>java.time.Instant</c> (Health Connect) and <c>NSDate</c> (HealthKit) have
    /// well-defined, tested behavior. A daily aggregation anchored at epoch produces ~20,000
    /// buckets to today — well within both platforms' tested capacity, and orders of magnitude
    /// below the failure threshold above.</para>
    ///
    /// <para><b>Why we don't pick a tighter, "more domain-relevant" floor.</b> The frameworks
    /// themselves are recent: <b>Apple HealthKit</b> shipped with iOS 8 on <b>2014-09-17</b>
    /// (announced WWDC 2014-06-02), and <b>Android Health Connect</b> went GA in <b>March 2023</b>
    /// after a May 2022 announcement and Nov 2022 beta, then was integrated into Android 14 on
    /// <b>2023-10-04</b>. So device-tracked native data realistically only exists from
    /// 2014-09-17 onward. However, both platforms also accept <b>manually entered</b> records
    /// with arbitrary backdated timestamps — a user can legitimately add a weight reading
    /// dated 2008. Clamping at a framework-shipped date would silently drop that data. Unix
    /// epoch is far enough back that no real user-entered record is excluded, while still
    /// rescuing the platform from sentinel-value blowups like <c>DateTimeOffset.MinValue</c>
    /// or <c>DateTime.MinValue</c>.</para>
    /// </remarks>
    public static readonly DateTimeOffset MinSupportedStartUtc =
        new(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly DateTimeOffset _startTime;

    /// <summary>
    /// Start time of the measurement period. Values below <see cref="MinSupportedStartUtc"/>
    /// are clamped up to that floor — see the constant's docs for the reasoning.
    /// </summary>
    public required DateTimeOffset StartTime
    {
        get => _startTime;
        init => _startTime = value < MinSupportedStartUtc ? MinSupportedStartUtc : value;
    }

    /// <summary>
    /// End time of the measurement period.
    /// </summary>
    public required DateTimeOffset EndTime { get; init; }

    /// <summary>
    /// Creates a HealthTimeRange from DateTime values.
    /// </summary>
    public static HealthTimeRange FromDateTime(DateTime startTime, DateTime endTime)
    {
        return new HealthTimeRange
        {
            StartTime = new DateTimeOffset(startTime),
            EndTime = new DateTimeOffset(endTime)
        };
    }

    /// <summary>
    /// Creates a HealthTimeRange from DateTimeOffset values.
    /// </summary>
    public static HealthTimeRange FromDateTimeOffset(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        return new HealthTimeRange
        {
            StartTime = startTime,
            EndTime = endTime
        };
    }
}
