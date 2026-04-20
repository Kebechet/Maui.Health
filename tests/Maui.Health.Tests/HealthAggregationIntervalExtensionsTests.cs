using Maui.Health.Enums;
using Maui.Health.Extensions;
using Xunit;

namespace Maui.Health.Tests;

public class HealthAggregationIntervalExtensionsTests
{
    [Theory]
    [InlineData(HealthAggregationInterval.Minute, 1)]
    [InlineData(HealthAggregationInterval.Hour, 60)]
    [InlineData(HealthAggregationInterval.Day, 1440)]
    public void ToTimeSpan_ValidInterval_ReturnsExpectedTimeSpan(HealthAggregationInterval interval, int expectedMinutes)
    {
        // Arrange

        // Act
        var result = interval.ToTimeSpan();

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), result);
    }

    [Fact]
    public void ToTimeSpan_InvalidInterval_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var interval = (HealthAggregationInterval)999;

        // Act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => interval.ToTimeSpan());

        // Assert
        Assert.Equal("interval", exception.ParamName);
    }
}
