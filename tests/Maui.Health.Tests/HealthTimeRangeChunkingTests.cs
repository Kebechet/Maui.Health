using Maui.Health.Extensions;
using Maui.Health.Models.Metrics;
using Xunit;

namespace Maui.Health.Tests;

public class HealthTimeRangeChunkingTests
{
    private static readonly DateTimeOffset _anchor = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SplitIntoChunks_RangeBelowCeiling_YieldsSingleChunkEqualToInput()
    {
        // Arrange
        var range = HealthTimeRange.FromDateTimeOffset(_anchor, _anchor.AddDays(100));

        // Act
        var chunks = range.SplitIntoChunks(TimeSpan.FromDays(1), maxBucketsPerCall: 5000).ToList();

        // Assert
        Assert.Single(chunks);
        Assert.Equal(range.StartTime, chunks[0].StartTime);
        Assert.Equal(range.EndTime, chunks[0].EndTime);
    }

    [Fact]
    public void SplitIntoChunks_RangeExactlyAtCeiling_YieldsSingleChunk()
    {
        // Arrange
        // 5000 daily buckets exactly fill the ceiling.
        var range = HealthTimeRange.FromDateTimeOffset(_anchor, _anchor.AddDays(5000));

        // Act
        var chunks = range.SplitIntoChunks(TimeSpan.FromDays(1), maxBucketsPerCall: 5000).ToList();

        // Assert
        Assert.Single(chunks);
        Assert.Equal(range.StartTime, chunks[0].StartTime);
        Assert.Equal(range.EndTime, chunks[0].EndTime);
    }

    [Fact]
    public void SplitIntoChunks_OneTickOverCeiling_YieldsTwoChunks()
    {
        // Arrange
        // 5000 buckets + 1 tick → must spill into a second chunk.
        var end = _anchor.AddDays(5000).AddTicks(1);
        var range = HealthTimeRange.FromDateTimeOffset(_anchor, end);

        // Act
        var chunks = range.SplitIntoChunks(TimeSpan.FromDays(1), maxBucketsPerCall: 5000).ToList();

        // Assert
        Assert.Equal(2, chunks.Count);
        Assert.Equal(_anchor, chunks[0].StartTime);
        Assert.Equal(_anchor.AddDays(5000), chunks[0].EndTime);
        Assert.Equal(_anchor.AddDays(5000), chunks[1].StartTime);
        Assert.Equal(end, chunks[1].EndTime);
    }

    [Fact]
    public void SplitIntoChunks_TwoFullChunks_YieldsTwoEqualSplits()
    {
        // Arrange
        var range = HealthTimeRange.FromDateTimeOffset(_anchor, _anchor.AddDays(10000));

        // Act
        var chunks = range.SplitIntoChunks(TimeSpan.FromDays(1), maxBucketsPerCall: 5000).ToList();

        // Assert
        Assert.Equal(2, chunks.Count);
        Assert.Equal(_anchor, chunks[0].StartTime);
        Assert.Equal(_anchor.AddDays(5000), chunks[0].EndTime);
        Assert.Equal(_anchor.AddDays(5000), chunks[1].StartTime);
        Assert.Equal(_anchor.AddDays(10000), chunks[1].EndTime);
    }

    [Fact]
    public void SplitIntoChunks_WideRange_ChunksAreContiguousAndCoverFullRange()
    {
        // Arrange
        // 25-year daily window — well above any single-call ceiling.
        var range = HealthTimeRange.FromDateTimeOffset(_anchor, _anchor.AddYears(25));

        // Act
        var chunks = range.SplitIntoChunks(TimeSpan.FromDays(1), maxBucketsPerCall: 5000).ToList();

        // Assert
        Assert.True(chunks.Count > 1);
        Assert.Equal(range.StartTime, chunks[0].StartTime);
        Assert.Equal(range.EndTime, chunks[^1].EndTime);
        for (var i = 1; i < chunks.Count; i++)
        {
            Assert.Equal(chunks[i - 1].EndTime, chunks[i].StartTime);
        }
    }

    [Fact]
    public void SplitIntoChunks_NoChunkExceedsBucketCeiling()
    {
        // Arrange
        var range = HealthTimeRange.FromDateTimeOffset(_anchor, _anchor.AddDays(12345));
        var interval = TimeSpan.FromDays(1);
        const int ceiling = 5000;

        // Act
        var chunks = range.SplitIntoChunks(interval, ceiling).ToList();

        // Assert
        foreach (var chunk in chunks)
        {
            var bucketCount = (int)Math.Ceiling((chunk.EndTime - chunk.StartTime) / interval);
            Assert.True(bucketCount <= ceiling, $"chunk produced {bucketCount} buckets, exceeds ceiling {ceiling}");
        }
    }

    [Fact]
    public void SplitIntoChunks_NonDailyInterval_ChunksAlignedOnIntervalMultiples()
    {
        // Arrange
        // 30-minute buckets, 6000-bucket window → splits into 5000 + 1000 buckets.
        var interval = TimeSpan.FromMinutes(30);
        var range = HealthTimeRange.FromDateTimeOffset(_anchor, _anchor + TimeSpan.FromMinutes(30 * 6000));

        // Act
        var chunks = range.SplitIntoChunks(interval, maxBucketsPerCall: 5000).ToList();

        // Assert
        Assert.Equal(2, chunks.Count);
        Assert.Equal(_anchor + TimeSpan.FromMinutes(30 * 5000), chunks[0].EndTime);
        Assert.Equal(chunks[0].EndTime, chunks[1].StartTime);
        Assert.Equal(range.EndTime, chunks[^1].EndTime);
    }

    [Fact]
    public void SplitIntoChunks_EmptyRange_YieldsSingleChunkEqualToInput()
    {
        // Arrange
        var range = HealthTimeRange.FromDateTimeOffset(_anchor, _anchor);

        // Act
        var chunks = range.SplitIntoChunks(TimeSpan.FromDays(1), maxBucketsPerCall: 5000).ToList();

        // Assert
        Assert.Single(chunks);
        Assert.Equal(_anchor, chunks[0].StartTime);
        Assert.Equal(_anchor, chunks[0].EndTime);
    }

    [Fact]
    public void SplitIntoChunks_InvertedRange_YieldsSingleChunkEqualToInput()
    {
        // Arrange
        // Inverted ranges aren't a contract violation here — pass the input through unchanged
        // so the platform call itself decides what to do (matches pre-chunking behavior).
        var range = HealthTimeRange.FromDateTimeOffset(_anchor.AddDays(10), _anchor);

        // Act
        var chunks = range.SplitIntoChunks(TimeSpan.FromDays(1), maxBucketsPerCall: 5000).ToList();

        // Assert
        Assert.Single(chunks);
        Assert.Equal(range.StartTime, chunks[0].StartTime);
        Assert.Equal(range.EndTime, chunks[0].EndTime);
    }

    [Fact]
    public void SplitIntoChunks_NonPositiveInterval_Throws()
    {
        // Arrange
        var range = HealthTimeRange.FromDateTimeOffset(_anchor, _anchor.AddDays(10));

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            range.SplitIntoChunks(TimeSpan.Zero, maxBucketsPerCall: 5000).ToList());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            range.SplitIntoChunks(TimeSpan.FromMinutes(-1), maxBucketsPerCall: 5000).ToList());
    }

    [Fact]
    public void SplitIntoChunks_FiveThousandDayChunk_Overflows5000CalendarDaysInDstZone()
    {
        // Reproduces SATISFIT-APP-5Q. The chunker walks fixed 24-hour slots, but Health Connect's
        // aggregateGroupByPeriod counts *calendar* days in the request timezone. Anchored at
        // local midnight in a DST zone, a 5000 fixed-day chunk lands at local-midnight+1h after a
        // net DST shift — Health Connect interprets that as 5001 calendar groups and throws
        // "Number of groups must not exceed 5000".

        // Arrange — 1970-01-01 00:00 Europe/Prague (CET, +01:00). 5000 fixed days later we land
        // in September 1983 with DST active (CEST, +02:00), so the local clock shows 01:00 — one
        // calendar boundary past local midnight.
        var prague = TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague");
        var startLocal = new DateTime(1970, 1, 1, 0, 0, 0);
        var start = new DateTimeOffset(startLocal, prague.GetUtcOffset(startLocal));
        var range = HealthTimeRange.FromDateTimeOffset(start, start.AddDays(20000));

        // Act
        var firstChunk = range.SplitIntoChunks(TimeSpan.FromDays(1), maxBucketsPerCall: 5000).First();

        // Assert
        var calendarDays = CountCalendarDaysInZone(firstChunk, prague);
        Assert.Equal(5001, calendarDays);
    }

    [Fact]
    public void SplitIntoChunks_With4999DayChunkInDstZone_StaysWithin5000CalendarDays()
    {
        // The fix for SATISFIT-APP-5Q: passing 4999 reserves one bucket of headroom. DST always
        // shifts by ±1 hour, so worst-case net drift inside a chunk pushes calendar-day count up
        // by at most one — 4999 + 1 = 5000 ≤ Health Connect's ceiling.

        // Arrange
        var prague = TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague");
        var startLocal = new DateTime(1970, 1, 1, 0, 0, 0);
        var start = new DateTimeOffset(startLocal, prague.GetUtcOffset(startLocal));
        var range = HealthTimeRange.FromDateTimeOffset(start, start.AddDays(20000));

        // Act
        var chunks = range.SplitIntoChunks(TimeSpan.FromDays(1), maxBucketsPerCall: 4999).ToList();

        // Assert
        foreach (var chunk in chunks)
        {
            var calendarDays = CountCalendarDaysInZone(chunk, prague);
            Assert.True(calendarDays <= 5000, $"Chunk {chunk.StartTime:o}..{chunk.EndTime:o} spans {calendarDays} calendar days in Europe/Prague; must fit Health Connect's 5000-group ceiling.");
        }
    }

    private static int CountCalendarDaysInZone(HealthTimeRange chunk, TimeZoneInfo timeZone)
    {
        var localStart = TimeZoneInfo.ConvertTime(chunk.StartTime, timeZone);
        var localEnd = TimeZoneInfo.ConvertTime(chunk.EndTime, timeZone);
        var fullDays = (localEnd.Date - localStart.Date).Days;
        // A non-midnight local end means the platform has to allocate a partial group on top of
        // the full days — same accounting aggregateGroupByPeriod uses internally.
        return fullDays + (localEnd.TimeOfDay > TimeSpan.Zero ? 1 : 0);
    }

    [Fact]
    public void SplitIntoChunks_NonPositiveCeiling_Throws()
    {
        // Arrange
        var range = HealthTimeRange.FromDateTimeOffset(_anchor, _anchor.AddDays(10));

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            range.SplitIntoChunks(TimeSpan.FromDays(1), maxBucketsPerCall: 0).ToList());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            range.SplitIntoChunks(TimeSpan.FromDays(1), maxBucketsPerCall: -1).ToList());
    }
}
