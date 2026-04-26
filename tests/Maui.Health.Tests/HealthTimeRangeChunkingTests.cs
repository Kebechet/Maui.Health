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
