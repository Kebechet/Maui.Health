using Maui.Health.Enums;
using Maui.Health.Models.Metrics;
using Maui.Health.Models.Metrics.Write;
using Xunit;

namespace Maui.Health.Tests;

public class NutritionDtoTests
{
    [Fact]
    public void Construction_WithAllFields_PopulatesEveryNutrient()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow;
        var end = start.AddMinutes(30);

        // Act
        var dto = new NutritionDto
        {
            Id = "n1",
            DataSdk = HealthDataSdk.GoogleHealthConnect,
            DataOrigin = "com.test",
            Timestamp = start,
            StartTime = start,
            EndTime = end,
            MealType = MealType.Lunch,
            Name = "Spaghetti Bolognese",
            Energy = 620.5,
            Protein = 25.4,
            TotalFat = 18.2,
            SaturatedFat = 6.1,
            Carbohydrates = 78.3,
            Sugar = 8.5,
            Fiber = 5.2,
            Sodium = 720,
            Cholesterol = 55,
        };

        // Assert
        Assert.Equal("n1", dto.Id);
        Assert.Equal(start, dto.StartTime);
        Assert.Equal(end, dto.EndTime);
        Assert.Equal(MealType.Lunch, dto.MealType);
        Assert.Equal("Spaghetti Bolognese", dto.Name);
        Assert.Equal(620.5, dto.Energy);
        Assert.Equal(25.4, dto.Protein);
        Assert.Equal(18.2, dto.TotalFat);
        Assert.Equal(6.1, dto.SaturatedFat);
        Assert.Equal(78.3, dto.Carbohydrates);
        Assert.Equal(8.5, dto.Sugar);
        Assert.Equal(5.2, dto.Fiber);
        Assert.Equal(720, dto.Sodium);
        Assert.Equal(55, dto.Cholesterol);
    }

    [Fact]
    public void Construction_OnlyRequiredFields_NutrientsAreNullAndMealTypeIsUnknown()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow;
        var end = start.AddMinutes(15);

        // Act
        var dto = new NutritionDto
        {
            Id = "n2",
            DataSdk = HealthDataSdk.AppleHealthKit,
            Timestamp = start,
            StartTime = start,
            EndTime = end,
        };

        // Assert
        Assert.Equal(MealType.Unknown, dto.MealType);
        Assert.Null(dto.Name);
        Assert.Null(dto.Energy);
        Assert.Null(dto.Protein);
        Assert.Null(dto.TotalFat);
        Assert.Null(dto.SaturatedFat);
        Assert.Null(dto.Carbohydrates);
        Assert.Null(dto.Sugar);
        Assert.Null(dto.Fiber);
        Assert.Null(dto.Sodium);
        Assert.Null(dto.Cholesterol);
    }

    [Fact]
    public void NutritionWriteData_Construction_PopulatesEveryNutrient()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow;
        var end = start.AddMinutes(20);

        // Act
        var write = new NutritionWriteData
        {
            StartTime = start,
            EndTime = end,
            MealType = MealType.Breakfast,
            Name = "Oatmeal",
            Energy = 320.0,
            Protein = 12.0,
            TotalFat = 6.5,
            SaturatedFat = 1.2,
            Carbohydrates = 55.0,
            Sugar = 10.0,
            Fiber = 8.0,
            Sodium = 150,
            Cholesterol = 0,
        };

        // Assert
        Assert.Equal(MealType.Breakfast, write.MealType);
        Assert.Equal("Oatmeal", write.Name);
        Assert.Equal(320.0, write.Energy);
        Assert.Equal(12.0, write.Protein);
        Assert.Equal(6.5, write.TotalFat);
        Assert.Equal(1.2, write.SaturatedFat);
        Assert.Equal(55.0, write.Carbohydrates);
        Assert.Equal(10.0, write.Sugar);
        Assert.Equal(8.0, write.Fiber);
        Assert.Equal(150, write.Sodium);
        Assert.Equal(0, write.Cholesterol);
    }

    [Fact]
    public void NutritionWriteData_OnlyRequiredFields_NutrientsAreNullAndMealTypeIsUnknown()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow;
        var end = start.AddMinutes(5);

        // Act
        var write = new NutritionWriteData
        {
            StartTime = start,
            EndTime = end,
        };

        // Assert
        Assert.Equal(MealType.Unknown, write.MealType);
        Assert.Null(write.Name);
        Assert.Null(write.Energy);
        Assert.Null(write.Protein);
        Assert.Null(write.TotalFat);
        Assert.Null(write.SaturatedFat);
        Assert.Null(write.Carbohydrates);
        Assert.Null(write.Sugar);
        Assert.Null(write.Fiber);
        Assert.Null(write.Sodium);
        Assert.Null(write.Cholesterol);
    }

    [Theory]
    // The integer values must match Health Connect's MEAL_TYPE_* constants so the Android
    // wiring can cast directly. Do not renumber.
    // https://developer.android.com/reference/androidx/health/connect/client/records/NutritionRecord#MEAL_TYPE_UNKNOWN()
    [InlineData(MealType.Unknown, 0)]
    [InlineData(MealType.Breakfast, 1)]
    [InlineData(MealType.Lunch, 2)]
    [InlineData(MealType.Dinner, 3)]
    [InlineData(MealType.Snack, 4)]
    public void MealType_OrdinalValue_MatchesHealthConnectConstant(MealType mealType, int expected)
    {
        // Arrange + Act
        var ordinal = (int)mealType;

        // Assert
        Assert.Equal(expected, ordinal);
    }
}
