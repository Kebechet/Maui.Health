using Maui.Health.Extensions;
using Xunit;

namespace Maui.Health.Tests;

public class MetricTimestampExtensionsTests
{
    [Fact]
    public void RebaseToOffset_NullOffset_ReturnsInputUnchanged()
    {
        // Arrange — null offset is the "source had no recording-zone metadata" path; falls
        // through to the UTC offset the input already carries.
        var utcInstant = new DateTimeOffset(2026, 5, 5, 22, 0, 0, TimeSpan.Zero);

        // Act
        var result = utcInstant.RebaseToOffset(null);

        // Assert
        Assert.Equal(utcInstant, result);
        Assert.Equal(TimeSpan.Zero, result.Offset);
    }

    [Fact]
    public void RebaseToOffset_PlusTwoHours_RebasesToCestMidnight()
    {
        // Arrange — the bug repro: a record taken at midnight CEST (UTC+2) lands as 22:00 UTC.
        // Rebasing to +02:00 must restore the recording wall-clock and offset.
        var utcInstant = new DateTimeOffset(2026, 5, 5, 22, 0, 0, TimeSpan.Zero);

        // Act
        var result = utcInstant.RebaseToOffset(TimeSpan.FromHours(2));

        // Assert
        Assert.Equal(TimeSpan.FromHours(2), result.Offset);
        Assert.Equal(new DateTime(2026, 5, 6, 0, 0, 0), result.DateTime);
        Assert.Equal(utcInstant.UtcDateTime, result.UtcDateTime);
    }

    [Fact]
    public void RebaseToOffset_NegativeOffset_RebasesBackwards()
    {
        // Arrange — UTC noon, recorded in EST (-05:00).
        var utcInstant = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

        // Act
        var result = utcInstant.RebaseToOffset(TimeSpan.FromHours(-5));

        // Assert
        Assert.Equal(TimeSpan.FromHours(-5), result.Offset);
        Assert.Equal(new DateTime(2026, 1, 15, 7, 0, 0), result.DateTime);
        Assert.Equal(utcInstant.UtcDateTime, result.UtcDateTime);
    }

    [Fact]
    public void RebaseToOffset_HalfHourOffset_HandlesFractionalZones()
    {
        // Arrange — Asia/Kolkata is +05:30; Health Connect ZoneOffset can carry such offsets.
        var utcInstant = new DateTimeOffset(2026, 5, 5, 18, 30, 0, TimeSpan.Zero);

        // Act
        var result = utcInstant.RebaseToOffset(TimeSpan.FromMinutes(330));

        // Assert
        Assert.Equal(new TimeSpan(5, 30, 0), result.Offset);
        Assert.Equal(new DateTime(2026, 5, 6, 0, 0, 0), result.DateTime);
    }

    [Fact]
    public void RebaseToOffset_PreservesAbsoluteInstant()
    {
        // Arrange
        var utcInstant = new DateTimeOffset(2026, 7, 1, 9, 30, 15, TimeSpan.Zero);

        // Act
        var result = utcInstant.RebaseToOffset(TimeSpan.FromHours(9));

        // Assert — only the carried offset changes; the moment must stay identical.
        Assert.Equal(utcInstant.UtcDateTime, result.UtcDateTime);
        Assert.Equal(TimeSpan.FromHours(9), result.Offset);
    }

    private static readonly TimeZoneInfo _bratislava = TimeZoneInfo.FindSystemTimeZoneById("Europe/Bratislava");
    private static readonly TimeZoneInfo _newYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    [Fact]
    public void RebaseToZone_CestMidnightExpressedInUtc_ReturnsCestOffset()
    {
        // Arrange — repro of the iOS aggregator output: a "May 6 midnight CEST" bucket comes
        // back as 2026-05-05 22:00 UTC because NSDate carries no zone info. After rebasing to
        // Bratislava, the value must surface as 2026-05-06 00:00 +02:00.
        var utcInstant = new DateTimeOffset(2026, 5, 5, 22, 0, 0, TimeSpan.Zero);

        // Act
        var result = utcInstant.RebaseToZone(_bratislava);

        // Assert
        Assert.Equal(TimeSpan.FromHours(2), result.Offset);
        Assert.Equal(new DateTime(2026, 5, 6, 0, 0, 0), result.DateTime);
        Assert.Equal(utcInstant.UtcDateTime, result.UtcDateTime);
    }

    [Fact]
    public void RebaseToZone_CetWinterDay_ReturnsPlusOneOffset()
    {
        // Arrange — Jan 15 is in CET (no DST).
        var utcInstant = new DateTimeOffset(2026, 1, 15, 23, 0, 0, TimeSpan.Zero);

        // Act
        var result = utcInstant.RebaseToZone(_bratislava);

        // Assert
        Assert.Equal(TimeSpan.FromHours(1), result.Offset);
        Assert.Equal(new DateTime(2026, 1, 16, 0, 0, 0), result.DateTime);
    }

    [Fact]
    public void RebaseToZone_OnDstSpringForwardMorning_UsesPostTransitionOffset()
    {
        // Arrange — March 30 2025 04:30 UTC is 06:30 CEST (post-transition that day).
        // Since the moment is past the 02→03 jump, the offset is +02:00.
        var utcInstant = new DateTimeOffset(2025, 3, 30, 4, 30, 0, TimeSpan.Zero);

        // Act
        var result = utcInstant.RebaseToZone(_bratislava);

        // Assert
        Assert.Equal(TimeSpan.FromHours(2), result.Offset);
        Assert.Equal(new DateTime(2025, 3, 30, 6, 30, 0), result.DateTime);
    }

    [Fact]
    public void RebaseToZone_OnDstFallBackEarlyMorning_UsesPreTransitionOffset()
    {
        // Arrange — Oct 26 2025 00:30 UTC is 02:30 CEST (pre-fallback). The fall-back happens
        // at 03:00 CEST → 02:00 CET. At 00:30 UTC we're still in CEST.
        var utcInstant = new DateTimeOffset(2025, 10, 26, 0, 30, 0, TimeSpan.Zero);

        // Act
        var result = utcInstant.RebaseToZone(_bratislava);

        // Assert
        Assert.Equal(TimeSpan.FromHours(2), result.Offset);
        Assert.Equal(new DateTime(2025, 10, 26, 2, 30, 0), result.DateTime);
    }

    [Fact]
    public void RebaseToZone_NewYorkInSummer_ReturnsMinusFourOffset()
    {
        // Arrange — June 15 2025 16:00 UTC is 12:00 EDT (-04:00).
        var utcInstant = new DateTimeOffset(2025, 6, 15, 16, 0, 0, TimeSpan.Zero);

        // Act
        var result = utcInstant.RebaseToZone(_newYork);

        // Assert
        Assert.Equal(TimeSpan.FromHours(-4), result.Offset);
        Assert.Equal(new DateTime(2025, 6, 15, 12, 0, 0), result.DateTime);
    }

    [Fact]
    public void RebaseToZone_NewYorkInWinter_ReturnsMinusFiveOffset()
    {
        // Arrange — Jan 15 2025 17:00 UTC is 12:00 EST (-05:00).
        var utcInstant = new DateTimeOffset(2025, 1, 15, 17, 0, 0, TimeSpan.Zero);

        // Act
        var result = utcInstant.RebaseToZone(_newYork);

        // Assert
        Assert.Equal(TimeSpan.FromHours(-5), result.Offset);
        Assert.Equal(new DateTime(2025, 1, 15, 12, 0, 0), result.DateTime);
    }

    [Fact]
    public void RebaseToZone_Utc_PreservesInputOffset()
    {
        // Arrange
        var utcInstant = new DateTimeOffset(2026, 5, 5, 22, 0, 0, TimeSpan.Zero);

        // Act
        var result = utcInstant.RebaseToZone(TimeZoneInfo.Utc);

        // Assert
        Assert.Equal(TimeSpan.Zero, result.Offset);
        Assert.Equal(utcInstant, result);
    }

    [Fact]
    public void RebaseToZone_NonUtcInputRebasedToBratislava_PreservesAbsoluteInstant()
    {
        // Arrange — input is already in some non-UTC offset (Tokyo +09:00). Must convert
        // to the equivalent CEST moment.
        var tokyoMoment = new DateTimeOffset(2026, 5, 6, 7, 0, 0, TimeSpan.FromHours(9));

        // Act
        var result = tokyoMoment.RebaseToZone(_bratislava);

        // Assert — same instant (May 5 22:00 UTC = May 6 07:00 Tokyo = May 6 00:00 CEST).
        Assert.Equal(tokyoMoment.UtcDateTime, result.UtcDateTime);
        Assert.Equal(TimeSpan.FromHours(2), result.Offset);
        Assert.Equal(new DateTime(2026, 5, 6, 0, 0, 0), result.DateTime);
    }
}
