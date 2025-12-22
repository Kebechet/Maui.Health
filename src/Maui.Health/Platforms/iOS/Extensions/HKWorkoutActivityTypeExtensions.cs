using HealthKit;
using Maui.Health.Constants;
using Maui.Health.Enums;
using Maui.Health.Models.Metrics;

namespace Maui.Health.Platforms.iOS.Extensions;

internal static class HKWorkoutActivityTypeExtensions
{
    /// <summary>
    /// Mapping from HKWorkoutActivityType (iOS) to ActivityType (cross-platform).
    /// HKWorkoutActivityType is the key because iOS has more specific workout types
    /// than our cross-platform ActivityType enum, so multiple HKWorkoutActivityTypes may map
    /// to the same ActivityType.
    /// </summary>
    private static readonly Dictionary<HKWorkoutActivityType, ActivityType> WorkoutActivityTypeMap = new()
    {
        { HKWorkoutActivityType.AmericanFootball, ActivityType.AmericanFootball },
        { HKWorkoutActivityType.Archery, ActivityType.Archery },
        { HKWorkoutActivityType.AustralianFootball, ActivityType.AustralianFootball },
        { HKWorkoutActivityType.Badminton, ActivityType.Badminton },
        { HKWorkoutActivityType.Baseball, ActivityType.Baseball },
        { HKWorkoutActivityType.Basketball, ActivityType.Basketball },
        { HKWorkoutActivityType.Bowling, ActivityType.Bowling },
        { HKWorkoutActivityType.Boxing, ActivityType.Boxing },
        { HKWorkoutActivityType.Climbing, ActivityType.Climbing },
        { HKWorkoutActivityType.Cricket, ActivityType.Cricket },
        { HKWorkoutActivityType.CrossTraining, ActivityType.CrossTraining },
        { HKWorkoutActivityType.Curling, ActivityType.Curling },
        { HKWorkoutActivityType.Cycling, ActivityType.Cycling },
        { HKWorkoutActivityType.Dance, ActivityType.Dance },
        { HKWorkoutActivityType.DanceInspiredTraining, ActivityType.DanceInspiredTraining },
        { HKWorkoutActivityType.Elliptical, ActivityType.Elliptical },
        { HKWorkoutActivityType.EquestrianSports, ActivityType.EquestrianSports },
        { HKWorkoutActivityType.Fencing, ActivityType.Fencing },
        { HKWorkoutActivityType.Fishing, ActivityType.Fishing },
        { HKWorkoutActivityType.FunctionalStrengthTraining, ActivityType.Weightlifting }, // not 1:1 mapping
        { HKWorkoutActivityType.Golf, ActivityType.Golf },
        { HKWorkoutActivityType.Gymnastics, ActivityType.Gymnastics },
        { HKWorkoutActivityType.Handball, ActivityType.Handball },
        { HKWorkoutActivityType.Hiking, ActivityType.Hiking },
        { HKWorkoutActivityType.Hockey, ActivityType.Hockey },
        { HKWorkoutActivityType.Hunting, ActivityType.Hunting },
        { HKWorkoutActivityType.Lacrosse, ActivityType.Lacrosse },
        { HKWorkoutActivityType.MartialArts, ActivityType.MartialArts },
        { HKWorkoutActivityType.MindAndBody, ActivityType.MindAndBody },
        { HKWorkoutActivityType.MixedMetabolicCardioTraining, ActivityType.MixedMetabolicCardioTraining },
        { HKWorkoutActivityType.PaddleSports, ActivityType.PaddleSports },
        { HKWorkoutActivityType.Play, ActivityType.Play },
        { HKWorkoutActivityType.PreparationAndRecovery, ActivityType.PreparationAndRecovery },
        { HKWorkoutActivityType.Racquetball, ActivityType.Racquetball },
        { HKWorkoutActivityType.Rowing, ActivityType.Rowing },
        { HKWorkoutActivityType.Rugby, ActivityType.Rugby },
        { HKWorkoutActivityType.Running, ActivityType.Running },
        { HKWorkoutActivityType.Sailing, ActivityType.Sailing },
        { HKWorkoutActivityType.SkatingSports, ActivityType.SkatingSports },
        { HKWorkoutActivityType.SnowSports, ActivityType.SnowSports },
        { HKWorkoutActivityType.Soccer, ActivityType.Soccer },
        { HKWorkoutActivityType.Softball, ActivityType.Softball },
        { HKWorkoutActivityType.Squash, ActivityType.Squash },
        { HKWorkoutActivityType.StairClimbing, ActivityType.StairClimbing },
        { HKWorkoutActivityType.SurfingSports, ActivityType.SurfingSports },
        { HKWorkoutActivityType.Swimming, ActivityType.Swimming },
        { HKWorkoutActivityType.TableTennis, ActivityType.TableTennis },
        { HKWorkoutActivityType.Tennis, ActivityType.Tennis },
        { HKWorkoutActivityType.TrackAndField, ActivityType.TrackAndField },
        { HKWorkoutActivityType.TraditionalStrengthTraining, ActivityType.StrengthTraining }, // not 1:1 mapping
        { HKWorkoutActivityType.Volleyball, ActivityType.Volleyball },
        { HKWorkoutActivityType.Walking, ActivityType.Walking },
        { HKWorkoutActivityType.WaterFitness, ActivityType.WaterFitness },
        { HKWorkoutActivityType.WaterPolo, ActivityType.WaterPolo },
        { HKWorkoutActivityType.WaterSports, ActivityType.WaterSports },
        { HKWorkoutActivityType.Wrestling, ActivityType.Wrestling },
        { HKWorkoutActivityType.Yoga, ActivityType.Yoga },
        { HKWorkoutActivityType.Barre, ActivityType.Barre },
        { HKWorkoutActivityType.CoreTraining, ActivityType.CoreTraining },
        { HKWorkoutActivityType.CrossCountrySkiing, ActivityType.Skiing }, // not 1:1 mapping
        { HKWorkoutActivityType.DownhillSkiing, ActivityType.Skiing }, // not 1:1 mapping
        { HKWorkoutActivityType.Flexibility, ActivityType.Stretching }, // not 1:1 mapping
        { HKWorkoutActivityType.HighIntensityIntervalTraining, ActivityType.HighIntensityIntervalTraining },
        { HKWorkoutActivityType.JumpRope, ActivityType.JumpRope },
        { HKWorkoutActivityType.Kickboxing, ActivityType.Kickboxing },
        { HKWorkoutActivityType.Pilates, ActivityType.Pilates },
        { HKWorkoutActivityType.Snowboarding, ActivityType.Snowboarding },
        { HKWorkoutActivityType.Stairs, ActivityType.Stairs },
        { HKWorkoutActivityType.StepTraining, ActivityType.StepTraining },
        { HKWorkoutActivityType.WheelchairWalkPace, ActivityType.WheelchairWalkPace },
        { HKWorkoutActivityType.WheelchairRunPace, ActivityType.WheelchairRunPace },
        { HKWorkoutActivityType.TaiChi, ActivityType.TaiChi },
        { HKWorkoutActivityType.MixedCardio, ActivityType.MixedCardio },
        { HKWorkoutActivityType.HandCycling, ActivityType.HandCycling },
        { HKWorkoutActivityType.DiscSports, ActivityType.DiscSports },
        { HKWorkoutActivityType.FitnessGaming, ActivityType.FitnessGaming },
        { HKWorkoutActivityType.CardioDance, ActivityType.Dance }, // not 1:1 mapping
        { HKWorkoutActivityType.SocialDance, ActivityType.Dance }, // not 1:1 mapping
        { HKWorkoutActivityType.Pickleball, ActivityType.Pickleball },
        { HKWorkoutActivityType.Cooldown, ActivityType.Cooldown },
        { HKWorkoutActivityType.SwimBikeRun, ActivityType.SwimBikeRun },
        { HKWorkoutActivityType.Transition, ActivityType.Transition },
        { HKWorkoutActivityType.UnderwaterDiving, ActivityType.UnderwaterDiving },
        { HKWorkoutActivityType.Other, ActivityType.Unknown }
    };

    internal static ActivityType ToActivityType(this HKWorkoutActivityType workoutActivityType)
    {
        if (WorkoutActivityTypeMap.TryGetValue(workoutActivityType, out var activityType))
        {
            return activityType;
        }

        return ActivityType.Unknown;
    }

    internal static HKWorkoutActivityType ToHKWorkoutActivityType(this ActivityType activityType)
    {
        var entry = WorkoutActivityTypeMap.FirstOrDefault(kvp => kvp.Value == activityType);
        if (!entry.Equals(default(KeyValuePair<HKWorkoutActivityType, ActivityType>)))
        {
            return entry.Key;
        }

        return HKWorkoutActivityType.Other;
    }

    internal static WorkoutDto ToWorkoutDto(this HKWorkout workout)
    {
        var startTime = workout.StartDate.ToDateTimeOffset();
        var endTime = workout.EndDate.ToDateTimeOffset();
        var activityType = workout.WorkoutActivityType.ToActivityType();

        double? energyBurned = null;
        if (workout.TotalEnergyBurned is not null)
        {
            energyBurned = workout.TotalEnergyBurned.GetDoubleValue(HKUnit.Kilocalorie);
        }

        double? distance = null;
        if (workout.TotalDistance != null)
        {
            distance = workout.TotalDistance.GetDoubleValue(HKUnit.Meter);
        }

        return new WorkoutDto
        {
            Id = workout.Uuid.ToString(),
            DataOrigin = workout.SourceRevision?.Source?.Name ?? DataOrigin.Unknown,
            Timestamp = startTime,
            ActivityType = activityType,
            StartTime = startTime,
            EndTime = endTime,
            EnergyBurned = energyBurned,
            Distance = distance
        };
    }
}
