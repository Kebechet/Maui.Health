using Maui.Health.Enums;
using Maui.Health.Models;
using Maui.Health.Models.Metrics;
using Maui.Health.Models.Metrics.Write;

namespace Maui.Health.Extensions;

/// <summary>
/// Extension methods for health metric DTOs (permission mapping, aggregation).
/// </summary>
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
    /// Maps a read DTO type to its corresponding HealthDataType enum value
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
    /// Maps a write DTO type to its corresponding HealthDataType enum value
    /// </summary>
    internal static HealthDataType GetWriteHealthDataType<TDto>() where TDto : IHealthWritable
    {
        return typeof(TDto).Name switch
        {
            nameof(StepsWriteData) => HealthDataType.Steps,
            nameof(WeightWriteData) => HealthDataType.Weight,
            nameof(HeightWriteData) => HealthDataType.Height,
            nameof(ActiveCaloriesBurnedWriteData) => HealthDataType.ActiveCaloriesBurned,
            nameof(HeartRateWriteData) => HealthDataType.HeartRate,
            nameof(BodyFatWriteData) => HealthDataType.BodyFat,
            nameof(Vo2MaxWriteData) => HealthDataType.Vo2Max,
            //nameof(BloodPressureWriteData) => HealthDataType.BloodPressure,
            _ => throw new NotSupportedException($"Write DTO type {typeof(TDto).Name} is not supported")
        };
    }

    /// <summary>
    /// Gets the read permission required for a specific read DTO type
    /// </summary>
    internal static HealthPermissionDto GetRequiredPermission<TDto>() where TDto : HealthMetricBase
    {
        return new HealthPermissionDto
        {
            HealthDataType = GetHealthDataType<TDto>(),
            PermissionType = PermissionType.Read
        };
    }

    /// <summary>
    /// Gets the write permission required for a specific write DTO type
    /// </summary>
    internal static HealthPermissionDto GetRequiredWritePermission<TDto>() where TDto : IHealthWritable
    {
        return new HealthPermissionDto
        {
            HealthDataType = GetWriteHealthDataType<TDto>(),
            PermissionType = PermissionType.Write
        };
    }
}