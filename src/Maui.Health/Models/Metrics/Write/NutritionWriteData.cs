using Maui.Health.Enums;

namespace Maui.Health.Models.Metrics.Write;

/// <summary>
/// Write DTO for a nutrition / food-intake record.
/// Each nutrient is nullable so callers only populate what they have.
/// Energy is in kilocalories; masses in grams; sodium/cholesterol in milligrams.
/// </summary>
public class NutritionWriteData : HealthWriteRangeData
{
    /// <summary>
    /// Type of meal this record represents.
    /// </summary>
    public MealType MealType { get; init; } = MealType.Unknown;

    /// <summary>
    /// Optional free-text name of the food or meal.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Total dietary energy, in kilocalories.
    /// </summary>
    public double? Energy { get; init; }

    /// <summary>
    /// Protein, in grams.
    /// </summary>
    public double? Protein { get; init; }

    /// <summary>
    /// Total fat, in grams.
    /// </summary>
    public double? TotalFat { get; init; }

    /// <summary>
    /// Saturated fat, in grams.
    /// </summary>
    public double? SaturatedFat { get; init; }

    /// <summary>
    /// Total carbohydrates, in grams.
    /// </summary>
    public double? Carbohydrates { get; init; }

    /// <summary>
    /// Sugar, in grams.
    /// </summary>
    public double? Sugar { get; init; }

    /// <summary>
    /// Dietary fiber, in grams.
    /// </summary>
    public double? Fiber { get; init; }

    /// <summary>
    /// Sodium, in milligrams.
    /// </summary>
    public double? Sodium { get; init; }

    /// <summary>
    /// Cholesterol, in milligrams.
    /// </summary>
    public double? Cholesterol { get; init; }
}
