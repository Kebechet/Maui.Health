using Maui.Health.Extensions;
using Xunit;

namespace Maui.Health.Tests;

public class CalendarDayAlignmentExtensionsTests
{
    private static readonly TimeZoneInfo _bratislava = TimeZoneInfo.FindSystemTimeZoneById("Europe/Bratislava");
    private static readonly TimeZoneInfo _newYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
    private static readonly TimeZoneInfo _kolkata = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

    [Fact]
    public void SnapToCalendarDayStart_AfternoonInCest_SnapsBackToCestMidnight()
    {
        // Arrange — repro of the SatisFIT bug: a sync at 20:51 CEST on May 8 used the sync
        // moment as windowStart, so every "day" bucket from the lib started at 20:51 instead
        // of local midnight. After snapping, the windowStart is 2025-05-08 00:00 +02:00.
        var afternoon = new DateTimeOffset(2025, 5, 8, 20, 51, 42, TimeSpan.FromHours(2));

        // Act
        var result = afternoon.SnapToCalendarDayStart(_bratislava);

        // Assert
        Assert.Equal(new DateTime(2025, 5, 8, 0, 0, 0), result.DateTime);
        Assert.Equal(TimeSpan.FromHours(2), result.Offset);
    }

    [Fact]
    public void SnapToCalendarDayStart_AlreadyAtCestMidnight_ReturnsSameInstant()
    {
        // Arrange
        var midnight = new DateTimeOffset(2025, 5, 8, 0, 0, 0, TimeSpan.FromHours(2));

        // Act
        var result = midnight.SnapToCalendarDayStart(_bratislava);

        // Assert
        Assert.Equal(midnight, result);
    }

    [Fact]
    public void SnapToCalendarDayStart_UtcInputForCestZone_ReturnsCestMidnightSameDay()
    {
        // Arrange — same instant as 2025-05-08 20:51:42 +02:00 expressed in UTC.
        var utcInstant = new DateTimeOffset(2025, 5, 8, 18, 51, 42, TimeSpan.Zero);

        // Act
        var result = utcInstant.SnapToCalendarDayStart(_bratislava);

        // Assert
        Assert.Equal(new DateTime(2025, 5, 8, 0, 0, 0), result.DateTime);
        Assert.Equal(TimeSpan.FromHours(2), result.Offset);
    }

    [Fact]
    public void SnapToCalendarDayStart_LateUtcButNextDayLocal_AlignsToLocalDate()
    {
        // Arrange — 23:30 UTC on May 8 is 01:30 May 9 in CEST. The local calendar day is May 9.
        var lateNightUtc = new DateTimeOffset(2025, 5, 8, 23, 30, 0, TimeSpan.Zero);

        // Act
        var result = lateNightUtc.SnapToCalendarDayStart(_bratislava);

        // Assert
        Assert.Equal(new DateTime(2025, 5, 9, 0, 0, 0), result.DateTime);
        Assert.Equal(TimeSpan.FromHours(2), result.Offset);
    }

    [Fact]
    public void SnapToCalendarDayStart_EarlyUtcButPreviousDayInNyc_AlignsToLocalDate()
    {
        // Arrange — 03:00 UTC on May 8 is 23:00 May 7 in NYC (EDT, UTC-4).
        var earlyMorningUtc = new DateTimeOffset(2025, 5, 8, 3, 0, 0, TimeSpan.Zero);

        // Act
        var result = earlyMorningUtc.SnapToCalendarDayStart(_newYork);

        // Assert
        Assert.Equal(new DateTime(2025, 5, 7, 0, 0, 0), result.DateTime);
        Assert.Equal(TimeSpan.FromHours(-4), result.Offset);
    }

    [Fact]
    public void SnapToCalendarDayStart_NewYorkInWinter_UsesEstOffset()
    {
        // Arrange
        var winterAfternoon = new DateTimeOffset(2025, 1, 15, 14, 0, 0, TimeSpan.FromHours(-5));

        // Act
        var result = winterAfternoon.SnapToCalendarDayStart(_newYork);

        // Assert
        Assert.Equal(new DateTime(2025, 1, 15, 0, 0, 0), result.DateTime);
        Assert.Equal(TimeSpan.FromHours(-5), result.Offset);
    }

    [Fact]
    public void SnapToCalendarDayStart_NewYorkInSummer_UsesEdtOffset()
    {
        // Arrange — June is in DST in NYC (EDT, UTC-4).
        var summerAfternoon = new DateTimeOffset(2025, 6, 15, 14, 0, 0, TimeSpan.FromHours(-4));

        // Act
        var result = summerAfternoon.SnapToCalendarDayStart(_newYork);

        // Assert
        Assert.Equal(new DateTime(2025, 6, 15, 0, 0, 0), result.DateTime);
        Assert.Equal(TimeSpan.FromHours(-4), result.Offset);
    }

    [Fact]
    public void SnapToCalendarDayStart_KolkataHalfHourZone_PreservesFractionalOffset()
    {
        // Arrange — Asia/Kolkata is +05:30 year-round.
        var afternoon = new DateTimeOffset(2025, 5, 8, 14, 0, 0, new TimeSpan(5, 30, 0));

        // Act
        var result = afternoon.SnapToCalendarDayStart(_kolkata);

        // Assert
        Assert.Equal(new DateTime(2025, 5, 8, 0, 0, 0), result.DateTime);
        Assert.Equal(new TimeSpan(5, 30, 0), result.Offset);
    }

    [Fact]
    public void SnapToCalendarDayStart_UtcZone_ReturnsUtcMidnight()
    {
        // Arrange
        var afternoon = new DateTimeOffset(2025, 5, 8, 14, 0, 0, TimeSpan.Zero);

        // Act
        var result = afternoon.SnapToCalendarDayStart(TimeZoneInfo.Utc);

        // Assert
        Assert.Equal(new DateTime(2025, 5, 8, 0, 0, 0), result.DateTime);
        Assert.Equal(TimeSpan.Zero, result.Offset);
    }

    [Fact]
    public void SnapToCalendarDayStart_OnDstSpringForwardDay_UsesPreTransitionOffset()
    {
        // Arrange — DST starts in Europe/Bratislava on 2025-03-30 (02:00 CET → 03:00 CEST).
        // Midnight that day is still CET (+01:00). Snapping a 10:00 CEST input must land on
        // CET midnight, not CEST midnight.
        var morningAfterTransition = new DateTimeOffset(2025, 3, 30, 10, 0, 0, TimeSpan.FromHours(2));

        // Act
        var result = morningAfterTransition.SnapToCalendarDayStart(_bratislava);

        // Assert
        Assert.Equal(new DateTime(2025, 3, 30, 0, 0, 0), result.DateTime);
        Assert.Equal(TimeSpan.FromHours(1), result.Offset);
    }

    [Fact]
    public void SnapToCalendarDayStart_OnDstFallBackDay_UsesPreFallbackOffset()
    {
        // Arrange — DST ends in Europe/Bratislava on 2025-10-26 (03:00 CEST → 02:00 CET).
        // Midnight that day is still CEST (+02:00); the fall-back happens later that morning.
        // Snapping an afternoon CET input must land on CEST midnight.
        var afternoonAfterFallback = new DateTimeOffset(2025, 10, 26, 15, 0, 0, TimeSpan.FromHours(1));

        // Act
        var result = afternoonAfterFallback.SnapToCalendarDayStart(_bratislava);

        // Assert
        Assert.Equal(new DateTime(2025, 10, 26, 0, 0, 0), result.DateTime);
        Assert.Equal(TimeSpan.FromHours(2), result.Offset);
    }

    [Fact]
    public void SnapToCalendarDayStart_PreservesDateOrderingAcrossInputs()
    {
        // Arrange — three CEST afternoons on consecutive days. Snapping must produce three
        // consecutive midnights in the same order, separated by exactly one day each.
        var day1 = new DateTimeOffset(2025, 5, 8, 14, 0, 0, TimeSpan.FromHours(2));
        var day2 = new DateTimeOffset(2025, 5, 9, 14, 0, 0, TimeSpan.FromHours(2));
        var day3 = new DateTimeOffset(2025, 5, 10, 14, 0, 0, TimeSpan.FromHours(2));

        // Act
        var snapped1 = day1.SnapToCalendarDayStart(_bratislava);
        var snapped2 = day2.SnapToCalendarDayStart(_bratislava);
        var snapped3 = day3.SnapToCalendarDayStart(_bratislava);

        // Assert
        Assert.Equal(TimeSpan.FromDays(1), snapped2 - snapped1);
        Assert.Equal(TimeSpan.FromDays(1), snapped3 - snapped2);
    }
}
