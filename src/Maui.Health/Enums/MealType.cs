namespace Maui.Health.Enums;

/// <summary>
/// Type of meal a nutrition record represents.
/// Android: maps to Health Connect <c>NutritionRecord.MealType</c> integer constants.
/// iOS: HealthKit has no direct meal-type analogue; may be surfaced via metadata when nutrition support ships for iOS.
/// https://developer.android.com/reference/androidx/health/connect/client/records/NutritionRecord#MEAL_TYPE_UNKNOWN()
/// </summary>
public enum MealType
{
    /// <summary>
    /// Meal type is unknown or not specified.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Breakfast.
    /// </summary>
    Breakfast = 1,

    /// <summary>
    /// Lunch.
    /// </summary>
    Lunch = 2,

    /// <summary>
    /// Dinner.
    /// </summary>
    Dinner = 3,

    /// <summary>
    /// A snack or other non-primary meal.
    /// </summary>
    Snack = 4,
}
