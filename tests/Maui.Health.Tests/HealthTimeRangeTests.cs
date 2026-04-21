using Maui.Health.Models.Metrics;
using Xunit;

namespace Maui.Health.Tests;

public class HealthTimeRangeTests
{
    [Fact]
    public void MinSupportedStartUtc_IsUnixEpoch()
    {
        // Arrange / Act
        var floor = HealthTimeRange.MinSupportedStartUtc;

        // Assert
        Assert.Equal(new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero), floor);
    }

    [Fact]
    public void StartTime_DateTimeOffsetMinValue_ClampsToFloor()
    {
        // Arrange / Act
        var range = new HealthTimeRange
        {
            StartTime = DateTimeOffset.MinValue,
            EndTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        };

        // Assert
        Assert.Equal(HealthTimeRange.MinSupportedStartUtc, range.StartTime);
    }

    [Fact]
    public void StartTime_AboveFloor_PassesThrough()
    {
        // Arrange
        var unchanged = new DateTimeOffset(2024, 5, 15, 12, 30, 0, TimeSpan.FromHours(2));

        // Act
        var range = new HealthTimeRange
        {
            StartTime = unchanged,
            EndTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        };

        // Assert
        Assert.Equal(unchanged, range.StartTime);
    }

    [Fact]
    public void StartTime_ExactlyAtFloor_PassesThrough()
    {
        // Arrange / Act
        var range = new HealthTimeRange
        {
            StartTime = HealthTimeRange.MinSupportedStartUtc,
            EndTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        };

        // Assert
        Assert.Equal(HealthTimeRange.MinSupportedStartUtc, range.StartTime);
    }

    [Fact]
    public void EndTime_BelowFloor_PassesThroughUnchanged()
    {
        // Arrange
        // EndTime is intentionally not clamped — the floor only applies to StartTime so that
        // an unworkable lower bound can't cause the aggregator to enumerate millions of
        // buckets. EndTime governs the upper edge of the range; clamping it would silently
        // shrink legitimate "now" queries.
        var ancientEnd = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        var range = new HealthTimeRange
        {
            StartTime = HealthTimeRange.MinSupportedStartUtc,
            EndTime = ancientEnd,
        };

        // Assert
        Assert.Equal(ancientEnd, range.EndTime);
    }

    [Fact]
    public void FromDateTimeOffset_BelowFloor_ClampsToFloor()
    {
        // Arrange / Act
        var range = HealthTimeRange.FromDateTimeOffset(
            new DateTimeOffset(1950, 6, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        // Assert
        Assert.Equal(HealthTimeRange.MinSupportedStartUtc, range.StartTime);
    }
}
