using Maui.Health.Enums;
using Maui.Health.Models;
using Maui.Health.Models.Metrics;

namespace Maui.Health.Extensions;

public static class MetricDtoExtensions
{
    /// <summary>
    /// Calculates the total calories from a collection of ActiveCaloriesBurnedDto records
    /// </summary>
    public static double TotalCalories(this IEnumerable<ActiveCaloriesBurnedDto> calories)
    {
        return calories.Sum(c => c.Energy);
    }

    /// <summary>
    /// Maps a DTO type to its corresponding HealthDataType enum value
    /// </summary>
    internal static HealthDataType GetHealthDataType<TDto>() where TDto : HealthMetricBase
    {
        return typeof(TDto).Name switch
        {
            nameof(StepsDto) => HealthDataType.Steps,
            nameof(WeightDto) => HealthDataType.Weight,
            nameof(HeightDto) => HealthDataType.Height,
            nameof(ActiveCaloriesBurnedDto) => HealthDataType.ActiveCaloriesBurned,
            nameof(HeartRateDto) => HealthDataType.HeartRate,
            nameof(WorkoutDto) => HealthDataType.ExerciseSession,
            nameof(BodyFatDto) => HealthDataType.BodyFat,
            nameof(Vo2MaxDto) => HealthDataType.Vo2Max,
            //nameof(BloodPressureDto) => HealthDataType.BloodPressure,
            _ => throw new NotSupportedException($"DTO type {typeof(TDto).Name} is not supported")
        };
    }

    /// <summary>
    /// Gets the permission required for a specific DTO type
    /// </summary>
    internal static HealthPermissionDto GetRequiredPermission<TDto>() where TDto : HealthMetricBase
    {
        return new HealthPermissionDto
        {
            HealthDataType = GetHealthDataType<TDto>(),
            PermissionType = PermissionType.Read
        };
    }
}