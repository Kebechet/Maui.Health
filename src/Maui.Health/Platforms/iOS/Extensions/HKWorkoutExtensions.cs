using Foundation;
using HealthKit;
using Maui.Health.Enums;
using Maui.Health.Models.Metrics;

namespace Maui.Health.Platforms.iOS.Extensions;

internal static class HKWorkoutExtensions
{
    /// <summary>
    /// Converts a WorkoutDto to an HKObject (HKWorkout) for writing to HealthKit
    /// </summary>
    public static HKObject? ToHKObject(this WorkoutDto workoutDto)
    {
        return workoutDto.ToHKWorkout();
    }

    /// <summary>
    /// Converts a WorkoutDto to an HKWorkout for writing to HealthKit
    /// Note: EndTime must not be null (only for completed workouts)
    /// </summary>
    public static HKWorkout? ToHKWorkout(this WorkoutDto dto)
    {
        // Can only write completed workouts (with EndTime) to HealthKit
        if (dto.EndTime is null)
        {
            return null;
        }

        var activityType = dto.ActivityType.ToHKWorkoutActivityType();
        var startDate = dto.StartTime.ToNSDate();
        var endDate = dto.EndTime.Value.ToNSDate();
        var duration = (dto.EndTime.Value - dto.StartTime).TotalSeconds;

        HKQuantity? totalEnergyBurned = null;
        if (dto.EnergyBurned is not null)
        {
            totalEnergyBurned = HKQuantity.FromQuantity(HKUnit.Kilocalorie, dto.EnergyBurned.Value);
        }

        HKQuantity? totalDistance = null;
        if (dto.Distance is not null)
        {
            totalDistance = HKQuantity.FromQuantity(HKUnit.Meter, dto.Distance.Value);
        }

        return HKWorkout.Create(
            activityType,
            startDate,
            endDate,
            duration,
            totalEnergyBurned,
            totalDistance,
            (NSDictionary?)null
        );
    }
}
