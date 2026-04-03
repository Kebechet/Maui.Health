using Maui.Health.Extensions;
using Maui.Health.Models.Metrics;
using Xunit;

namespace Maui.Health.Tests;

public class MetricDtoExtensionsTests
{
    [Fact]
    public void TotalCalories_MultipleRecords_SumsEnergy()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var calories = new List<ActiveCaloriesBurnedDto>
        {
            new() { Id = "1", DataOrigin = "test", Timestamp = now, Energy = 100.5, Unit = "kcal", StartTime = now, EndTime = now.AddMinutes(30) },
            new() { Id = "2", DataOrigin = "test", Timestamp = now, Energy = 200.3, Unit = "kcal", StartTime = now, EndTime = now.AddMinutes(30) },
            new() { Id = "3", DataOrigin = "test", Timestamp = now, Energy = 50.2, Unit = "kcal", StartTime = now, EndTime = now.AddMinutes(30) },
        };

        // Act
        var total = calories.TotalCalories();

        // Assert
        Assert.Equal(351.0, total, precision: 1);
    }

    [Fact]
    public void TotalCalories_EmptyList_ReturnsZero()
    {
        // Arrange
        var calories = new List<ActiveCaloriesBurnedDto>();

        // Act
        var total = calories.TotalCalories();

        // Assert
        Assert.Equal(0, total);
    }

    [Fact]
    public void TotalCalories_SingleRecord_ReturnsSameValue()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var calories = new List<ActiveCaloriesBurnedDto>
        {
            new() { Id = "1", DataOrigin = "test", Timestamp = now, Energy = 42.7, Unit = "kcal", StartTime = now, EndTime = now.AddMinutes(30) },
        };

        // Act
        var total = calories.TotalCalories();

        // Assert
        Assert.Equal(42.7, total);
    }
}
