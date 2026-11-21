using Kebechet.Types.Health.Extensions;
using Kebechet.Types.Health.Models;
using Xunit;

namespace Maui.Health.Tests;

public class DateRangeExtensionsTests
{
    [Fact]
    public void ToJson_MultipleRanges_RoundTrips()
    {
        // Arrange
        var original = new List<DateRange>
        {
            new(DateTimeOffset.FromUnixTimeMilliseconds(1000), DateTimeOffset.FromUnixTimeMilliseconds(2000)),
            new(DateTimeOffset.FromUnixTimeMilliseconds(3000), DateTimeOffset.FromUnixTimeMilliseconds(4000)),
        };

        // Act
        var json = original.ToJson();
        var result = json.ToDateRanges();

        // Assert
        Assert.Equal(original.Count, result.Count);
        for (var i = 0; i < original.Count; i++)
        {
            Assert.Equal(original[i].Start, result[i].Start);
            Assert.Equal(original[i].End, result[i].End);
        }
    }

    [Fact]
    public void ToJson_Ranges_WritesTheFormatStoredByEarlierVersions()
    {
        // Arrange
        var ranges = new List<DateRange>
        {
            new(DateTimeOffset.FromUnixTimeMilliseconds(1000), DateTimeOffset.FromUnixTimeMilliseconds(2000)),
            new(DateTimeOffset.FromUnixTimeMilliseconds(3000)),
        };

        // Act
        var json = ranges.ToJson();

        // Assert
        Assert.Equal("""[{"Start":1000,"End":2000},{"Start":3000,"End":null}]""", json);
    }

    [Fact]
    public void ToDateRanges_FormatStoredByEarlierVersions_ReadsEveryRange()
    {
        // Arrange
        var json = """[{"Start":1000,"End":2000},{"Start":3000,"End":null}]""";

        // Act
        var ranges = json.ToDateRanges();

        // Assert
        Assert.Equal(2, ranges.Count);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1000), ranges[0].Start);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(2000), ranges[0].End);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(3000), ranges[1].Start);
        Assert.Null(ranges[1].End);
    }

    [Fact]
    public void ToJson_NullEnd_RoundTrips()
    {
        // Arrange
        var original = new List<DateRange>
        {
            new(DateTimeOffset.FromUnixTimeMilliseconds(1000)),
        };

        // Act
        var json = original.ToJson();
        var result = json.ToDateRanges();

        // Assert
        Assert.Single(result);
        Assert.Equal(original[0].Start, result[0].Start);
        Assert.Null(result[0].End);
    }

    [Fact]
    public void ToJson_EmptyList_RoundTrips()
    {
        // Arrange
        var original = new List<DateRange>();

        // Act
        var json = original.ToJson();
        var result = json.ToDateRanges();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ToDateRanges_EmptyString_ReturnsEmpty()
    {
        // Arrange
        var input = "";

        // Act
        var result = input.ToDateRanges();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ToDateRanges_NullString_ReturnsEmpty()
    {
        // Arrange
        string? input = null;

        // Act
        var result = input!.ToDateRanges();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ToDateRanges_InvalidJson_ReturnsEmpty()
    {
        // Arrange
        var input = "not json";

        // Act
        var result = input.ToDateRanges();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ToDateRanges_ObjectOverload_MatchesStringOverload()
    {
        // Arrange
        var original = new List<DateRange>
        {
            new(DateTimeOffset.FromUnixTimeMilliseconds(5000), DateTimeOffset.FromUnixTimeMilliseconds(6000)),
            new(DateTimeOffset.FromUnixTimeMilliseconds(7000)),
        };
        var json = original.ToJson();

        // Act
        var fromString = json.ToDateRanges();
        var fromObject = ((object)json).ToDateRanges();

        // Assert
        Assert.Equal(fromString.Count, fromObject.Count);
        for (var i = 0; i < fromString.Count; i++)
        {
            Assert.Equal(fromString[i].Start, fromObject[i].Start);
            Assert.Equal(fromString[i].End, fromObject[i].End);
        }
    }
}
