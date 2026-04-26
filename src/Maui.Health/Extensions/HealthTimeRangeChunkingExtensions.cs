using Maui.Health.Models.Metrics;

namespace Maui.Health.Extensions;

/// <summary>
/// Splits a <see cref="HealthTimeRange"/> into smaller sub-ranges so an interval-aggregation
/// call stays under a platform-imposed bucket-count ceiling.
/// </summary>
public static class HealthTimeRangeChunkingExtensions
{
    /// <summary>
    /// Splits <paramref name="timeRange"/> into half-open <c>[start, end)</c> sub-ranges so that
    /// each sub-range covers at most <paramref name="maxBucketsPerCall"/> buckets when aggregated
    /// with the given <paramref name="interval"/>. Sub-ranges are contiguous and aligned on
    /// <c>interval</c> boundaries (each chunk starts where the previous chunk ended), so
    /// concatenating their bucket lists produces the same sequence the platform would emit if
    /// the ceiling didn't exist. The final sub-range terminates exactly at
    /// <see cref="HealthTimeRange.EndTime"/>.
    /// </summary>
    /// <remarks>
    /// <para>An empty or inverted input range (<c>EndTime &lt;= StartTime</c>) yields a single
    /// sub-range equal to the input — leaving downstream behavior identical to the un-chunked
    /// path.</para>
    /// <para>The helper is platform-agnostic: callers pass the ceiling appropriate to their
    /// aggregator (e.g. Health Connect's undocumented 5000-bucket cap on
    /// <c>aggregateGroupByDuration</c>).</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="interval"/> is not positive or
    /// <paramref name="maxBucketsPerCall"/> is not positive.
    /// </exception>
    public static IEnumerable<HealthTimeRange> SplitIntoChunks(
        this HealthTimeRange timeRange,
        TimeSpan interval,
        int maxBucketsPerCall)
    {
        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), interval, "Must be greater than zero.");
        }
        if (maxBucketsPerCall <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxBucketsPerCall), maxBucketsPerCall, "Must be greater than zero.");
        }

        if (timeRange.EndTime <= timeRange.StartTime)
        {
            yield return timeRange;
            yield break;
        }

        var chunkDuration = TimeSpan.FromTicks(interval.Ticks * maxBucketsPerCall);
        var chunkStart = timeRange.StartTime;

        while (chunkStart < timeRange.EndTime)
        {
            var chunkEnd = chunkStart + chunkDuration;
            if (chunkEnd > timeRange.EndTime)
            {
                chunkEnd = timeRange.EndTime;
            }

            yield return HealthTimeRange.FromDateTimeOffset(chunkStart, chunkEnd);
            chunkStart = chunkEnd;
        }
    }
}
