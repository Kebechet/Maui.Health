using Maui.Health.Extensions;
using Xunit;

namespace Maui.Health.Tests;

public class AggregationIntervalExtensionsTests
{
    [Fact]
    public void TryGetWholeDayCount_OneDay_ReturnsTrueAndOne()
    {
        // Arrange — the SatisFIT case: TimeSpan.FromDays(1).
        var interval = TimeSpan.FromDays(1);

        // Act
        var isWholeDays = interval.TryGetWholeDayCount(out var days);

        // Assert
        Assert.True(isWholeDays);
        Assert.Equal(1, days);
    }

    [Fact]
    public void TryGetWholeDayCount_OneWeek_ReturnsTrueAndSeven()
    {
        // Arrange — weekly aggregation use case.
        var interval = TimeSpan.FromDays(7);

        // Act
        var isWholeDays = interval.TryGetWholeDayCount(out var days);

        // Assert
        Assert.True(isWholeDays);
        Assert.Equal(7, days);
    }

    [Fact]
    public void TryGetWholeDayCount_TwentyFourHours_ReturnsTrueAndOne()
    {
        // Arrange — TimeSpan.FromHours(24) is equivalent to 1 day; should classify the same.
        var interval = TimeSpan.FromHours(24);

        // Act
        var isWholeDays = interval.TryGetWholeDayCount(out var days);

        // Assert
        Assert.True(isWholeDays);
        Assert.Equal(1, days);
    }

    [Fact]
    public void TryGetWholeDayCount_FortyEightHours_ReturnsTrueAndTwo()
    {
        // Arrange
        var interval = TimeSpan.FromHours(48);

        // Act
        var isWholeDays = interval.TryGetWholeDayCount(out var days);

        // Assert
        Assert.True(isWholeDays);
        Assert.Equal(2, days);
    }

    [Fact]
    public void TryGetWholeDayCount_ThirtyDaysApproxMonth_ReturnsTrueAndThirty()
    {
        // Arrange — 30 days is a common "month-ish" granularity. Should still classify as
        // calendar-aware so DST transitions inside a month don't shift the bucket boundary
        // by an hour twice a year.
        var interval = TimeSpan.FromDays(30);

        // Act
        var isWholeDays = interval.TryGetWholeDayCount(out var days);

        // Assert
        Assert.True(isWholeDays);
        Assert.Equal(30, days);
    }

    [Fact]
    public void TryGetWholeDayCount_OneHour_ReturnsFalse()
    {
        // Arrange — hourly aggregation must NOT use the calendar path; Period doesn't
        // support sub-day units (Period.OfHours doesn't exist; Period is days/months/years only).
        var interval = TimeSpan.FromHours(1);

        // Act
        var isWholeDays = interval.TryGetWholeDayCount(out _);

        // Assert
        Assert.False(isWholeDays);
    }

    [Fact]
    public void TryGetWholeDayCount_FifteenMinutes_ReturnsFalse()
    {
        // Arrange
        var interval = TimeSpan.FromMinutes(15);

        // Act
        var isWholeDays = interval.TryGetWholeDayCount(out _);

        // Assert
        Assert.False(isWholeDays);
    }

    [Fact]
    public void TryGetWholeDayCount_ThirtyMinutes_ReturnsFalse()
    {
        // Arrange
        var interval = TimeSpan.FromMinutes(30);

        // Act
        var isWholeDays = interval.TryGetWholeDayCount(out _);

        // Assert
        Assert.False(isWholeDays);
    }

    [Fact]
    public void TryGetWholeDayCount_TwentyFiveHours_ReturnsFalse()
    {
        // Arrange — 25 hours is more than 1 day but not a whole-day multiple. Period.OfDays
        // would lose the extra hour; classification must reject it so the duration path runs.
        var interval = TimeSpan.FromHours(25);

        // Act
        var isWholeDays = interval.TryGetWholeDayCount(out _);

        // Assert
        Assert.False(isWholeDays);
    }

    [Fact]
    public void TryGetWholeDayCount_OneDayPlusOneMinute_ReturnsFalse()
    {
        // Arrange — exactly off by one minute past 1 day. Must fall through to the duration path.
        var interval = TimeSpan.FromDays(1) + TimeSpan.FromMinutes(1);

        // Act
        var isWholeDays = interval.TryGetWholeDayCount(out _);

        // Assert
        Assert.False(isWholeDays);
    }

    [Fact]
    public void TryGetWholeDayCount_OneDayMinusOneMinute_ReturnsFalse()
    {
        // Arrange — 23h 59m. Just under a day.
        var interval = TimeSpan.FromDays(1) - TimeSpan.FromMinutes(1);

        // Act
        var isWholeDays = interval.TryGetWholeDayCount(out _);

        // Assert
        Assert.False(isWholeDays);
    }

    [Fact]
    public void TryGetWholeDayCount_Zero_ReturnsFalse()
    {
        // Arrange — guard against degenerate input. Caller's interval > 0 is validated
        // separately, but classification must not say "0 days, calendar path" for it.
        var interval = TimeSpan.Zero;

        // Act
        var isWholeDays = interval.TryGetWholeDayCount(out _);

        // Assert
        Assert.False(isWholeDays);
    }

    [Fact]
    public void TryGetWholeDayCount_NegativeOneDay_ReturnsFalse()
    {
        // Arrange — negative durations are nonsense for aggregation; classification rejects them.
        var interval = TimeSpan.FromDays(-1);

        // Act
        var isWholeDays = interval.TryGetWholeDayCount(out _);

        // Assert
        Assert.False(isWholeDays);
    }

    [Fact]
    public void TryGetWholeDayCount_OneSecond_ReturnsFalse()
    {
        // Arrange
        var interval = TimeSpan.FromSeconds(1);

        // Act
        var isWholeDays = interval.TryGetWholeDayCount(out _);

        // Assert
        Assert.False(isWholeDays);
    }
}
