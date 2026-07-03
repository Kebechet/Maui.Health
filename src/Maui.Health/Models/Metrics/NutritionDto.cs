using Maui.Health.Enums;

namespace Maui.Health.Models.Metrics;

/// <summary>
/// Represents a nutrition/food-intake record from health platforms.
/// A single record captures the nutrients consumed during a meal (breakfast, lunch, dinner, snack)
/// or any other food-intake event.
/// Android: maps to Health Connect <c>NutritionRecord</c> (single record with all nutrients as optional fields).
/// iOS: maps to a group of dietary <c>HKQuantitySample</c>s grouped by an <c>HKCorrelation</c> of type Food
/// (iOS support ships in a follow-up; the DTO shape is defined here so callers can share code).
/// </summary>
public class NutritionDto : HealthMetricBase, IHealthTimeRange
{
    /// <summary>
    /// Start time of the meal / food-intake period.
    /// </summary>
    public required DateTimeOffset StartTime { get; init; }

    /// <summary>
    /// End time of the meal / food-intake period.
    /// </summary>
    public required DateTimeOffset EndTime { get; init; }

    /// <summary>
    /// Type of meal this record represents (breakfast, lunch, dinner, snack, or unknown).
    /// </summary>
    public MealType MealType { get; init; } = MealType.Unknown;

    /// <summary>
    /// Optional free-text name of the food or meal (e.g. "Spaghetti Bolognese").
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Total dietary energy consumed, in <see cref="Constants.Units.Kilocalorie"/>.
    /// </summary>
    public double? Energy { get; init; }

    /// <summary>
    /// Protein consumed, in <see cref="Constants.Units.Gram"/>.
    /// </summary>
    public double? Protein { get; init; }

    /// <summary>
    /// Total fat consumed, in <see cref="Constants.Units.Gram"/>.
    /// </summary>
    public double? TotalFat { get; init; }

    /// <summary>
    /// Saturated fat consumed, in <see cref="Constants.Units.Gram"/>.
    /// </summary>
    public double? SaturatedFat { get; init; }

    /// <summary>
    /// Total carbohydrates consumed, in <see cref="Constants.Units.Gram"/>.
    /// </summary>
    public double? Carbohydrates { get; init; }

    /// <summary>
    /// Sugar consumed, in <see cref="Constants.Units.Gram"/>.
    /// </summary>
    public double? Sugar { get; init; }

    /// <summary>
    /// Dietary fiber consumed, in <see cref="Constants.Units.Gram"/>.
    /// </summary>
    public double? Fiber { get; init; }

    /// <summary>
    /// Sodium consumed, in <see cref="Constants.Units.Milligram"/>.
    /// </summary>
    public double? Sodium { get; init; }

    /// <summary>
    /// Cholesterol consumed, in <see cref="Constants.Units.Milligram"/>.
    /// </summary>
    public double? Cholesterol { get; init; }
}
