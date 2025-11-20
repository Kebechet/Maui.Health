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
        if (!dto.EndTime.HasValue)
            return null;

        var activityType = dto.ActivityType.ToHKWorkoutActivityType();
        var startDate = dto.StartTime.ToNSDate();
        var endDate = dto.EndTime.Value.ToNSDate();
        var duration = (dto.EndTime.Value - dto.StartTime).TotalSeconds;

        HKQuantity? totalEnergyBurned = null;
        if (dto.EnergyBurned.HasValue)
        {
            totalEnergyBurned = HKQuantity.FromQuantity(HKUnit.Kilocalorie, dto.EnergyBurned.Value);
        }

        HKQuantity? totalDistance = null;
        if (dto.Distance.HasValue)
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

    public static HKWorkoutActivityType ToHKWorkoutActivityType(this ActivityType activityType)
    {
        return activityType switch
        {
            ActivityType.Running => HKWorkoutActivityType.Running,
            ActivityType.Cycling => HKWorkoutActivityType.Cycling,
            ActivityType.Walking => HKWorkoutActivityType.Walking,
            ActivityType.Swimming => HKWorkoutActivityType.Swimming,
            ActivityType.Hiking => HKWorkoutActivityType.Hiking,
            ActivityType.Yoga => HKWorkoutActivityType.Yoga,
            ActivityType.StrengthTraining => HKWorkoutActivityType.TraditionalStrengthTraining,
            ActivityType.Calisthenics => HKWorkoutActivityType.FunctionalStrengthTraining,
            ActivityType.Elliptical => HKWorkoutActivityType.Elliptical,
            ActivityType.Rowing => HKWorkoutActivityType.Rowing,
            ActivityType.Pilates => HKWorkoutActivityType.Pilates,
            ActivityType.Dance => HKWorkoutActivityType.Dance,
            ActivityType.Soccer => HKWorkoutActivityType.Soccer,
            ActivityType.Basketball => HKWorkoutActivityType.Basketball,
            ActivityType.Baseball => HKWorkoutActivityType.Baseball,
            ActivityType.Tennis => HKWorkoutActivityType.Tennis,
            ActivityType.Golf => HKWorkoutActivityType.Golf,
            ActivityType.Badminton => HKWorkoutActivityType.Badminton,
            ActivityType.TableTennis => HKWorkoutActivityType.TableTennis,
            ActivityType.Volleyball => HKWorkoutActivityType.Volleyball,
            ActivityType.Cricket => HKWorkoutActivityType.Cricket,
            ActivityType.Rugby => HKWorkoutActivityType.Rugby,
            ActivityType.AmericanFootball => HKWorkoutActivityType.AmericanFootball,
            ActivityType.Skiing => HKWorkoutActivityType.DownhillSkiing,
            ActivityType.Snowboarding => HKWorkoutActivityType.Snowboarding,
            ActivityType.SurfingSports => HKWorkoutActivityType.SurfingSports,
            ActivityType.Sailing => HKWorkoutActivityType.Sailing,
            ActivityType.MartialArts => HKWorkoutActivityType.MartialArts,
            ActivityType.Boxing => HKWorkoutActivityType.Boxing,
            ActivityType.Wrestling => HKWorkoutActivityType.Wrestling,
            ActivityType.Climbing => HKWorkoutActivityType.Climbing,
            ActivityType.CrossTraining => HKWorkoutActivityType.CrossTraining,
            ActivityType.StairClimbing => HKWorkoutActivityType.StairClimbing,
            ActivityType.JumpRope => HKWorkoutActivityType.JumpRope,
            ActivityType.Unknown => HKWorkoutActivityType.Other,
            _ => HKWorkoutActivityType.Other
        };
    }
}
