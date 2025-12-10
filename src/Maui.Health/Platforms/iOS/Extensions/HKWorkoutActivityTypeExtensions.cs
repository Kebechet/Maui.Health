using HealthKit;
using Maui.Health.Constants;
using Maui.Health.Enums;
using Maui.Health.Models.Metrics;

namespace Maui.Health.Platforms.iOS.Extensions;

internal static class HKWorkoutActivityTypeExtensions
{
    /// <summary>
    /// Bidirectional mapping between ActivityType (key) and HKWorkoutActivityType (value).
    /// To convert ActivityType -> HKWorkoutActivityType: lookup by key.
    /// To convert HKWorkoutActivityType -> ActivityType: search by value.
    /// </summary>
    private static readonly Dictionary<ActivityType, HKWorkoutActivityType> ActivityTypeMap = new()
    {
        { ActivityType.AmericanFootball, HKWorkoutActivityType.AmericanFootball },
        { ActivityType.Archery, HKWorkoutActivityType.Archery },
        { ActivityType.AustralianFootball, HKWorkoutActivityType.AustralianFootball },
        { ActivityType.Badminton, HKWorkoutActivityType.Badminton },
        { ActivityType.Baseball, HKWorkoutActivityType.Baseball },
        { ActivityType.Basketball, HKWorkoutActivityType.Basketball },
        { ActivityType.Bowling, HKWorkoutActivityType.Bowling },
        { ActivityType.Boxing, HKWorkoutActivityType.Boxing },
        { ActivityType.Climbing, HKWorkoutActivityType.Climbing },
        { ActivityType.Cricket, HKWorkoutActivityType.Cricket },
        { ActivityType.CrossTraining, HKWorkoutActivityType.CrossTraining },
        { ActivityType.Curling, HKWorkoutActivityType.Curling },
        { ActivityType.Cycling, HKWorkoutActivityType.Cycling },
        { ActivityType.Dance, HKWorkoutActivityType.Dance },
        { ActivityType.DanceInspiredTraining, HKWorkoutActivityType.Dance }, // not 1:1 mapping
        { ActivityType.Elliptical, HKWorkoutActivityType.Elliptical },
        { ActivityType.EquestrianSports, HKWorkoutActivityType.EquestrianSports },
        { ActivityType.Fencing, HKWorkoutActivityType.Fencing },
        { ActivityType.Fishing, HKWorkoutActivityType.Fishing },
        { ActivityType.Weightlifting, HKWorkoutActivityType.FunctionalStrengthTraining }, // not 1:1 mapping
        { ActivityType.Golf, HKWorkoutActivityType.Golf },
        { ActivityType.Gymnastics, HKWorkoutActivityType.Gymnastics },
        { ActivityType.Handball, HKWorkoutActivityType.Handball },
        { ActivityType.Hiking, HKWorkoutActivityType.Hiking },
        { ActivityType.Hockey, HKWorkoutActivityType.Hockey },
        { ActivityType.Hunting, HKWorkoutActivityType.Hunting },
        { ActivityType.Lacrosse, HKWorkoutActivityType.Lacrosse },
        { ActivityType.MartialArts, HKWorkoutActivityType.MartialArts },
        { ActivityType.MindAndBody, HKWorkoutActivityType.MindAndBody },
        { ActivityType.MixedMetabolicCardioTraining, HKWorkoutActivityType.MixedCardio }, // not 1:1 mapping
        { ActivityType.PaddleSports, HKWorkoutActivityType.PaddleSports },
        { ActivityType.Play, HKWorkoutActivityType.Play },
        { ActivityType.PreparationAndRecovery, HKWorkoutActivityType.PreparationAndRecovery },
        { ActivityType.Racquetball, HKWorkoutActivityType.Racquetball },
        { ActivityType.Rowing, HKWorkoutActivityType.Rowing },
        { ActivityType.Rugby, HKWorkoutActivityType.Rugby },
        { ActivityType.Running, HKWorkoutActivityType.Running },
        { ActivityType.Sailing, HKWorkoutActivityType.Sailing },
        { ActivityType.SkatingSports, HKWorkoutActivityType.SkatingSports },
        { ActivityType.SnowSports, HKWorkoutActivityType.SnowSports },
        { ActivityType.Soccer, HKWorkoutActivityType.Soccer },
        { ActivityType.Softball, HKWorkoutActivityType.Softball },
        { ActivityType.Squash, HKWorkoutActivityType.Squash },
        { ActivityType.StairClimbing, HKWorkoutActivityType.StairClimbing },
        { ActivityType.SurfingSports, HKWorkoutActivityType.SurfingSports },
        { ActivityType.Swimming, HKWorkoutActivityType.Swimming },
        { ActivityType.TableTennis, HKWorkoutActivityType.TableTennis },
        { ActivityType.Tennis, HKWorkoutActivityType.Tennis },
        { ActivityType.TrackAndField, HKWorkoutActivityType.TrackAndField },
        { ActivityType.StrengthTraining, HKWorkoutActivityType.TraditionalStrengthTraining }, // not 1:1 mapping
        { ActivityType.Volleyball, HKWorkoutActivityType.Volleyball },
        { ActivityType.Walking, HKWorkoutActivityType.Walking },
        { ActivityType.WaterFitness, HKWorkoutActivityType.WaterFitness },
        { ActivityType.WaterPolo, HKWorkoutActivityType.WaterPolo },
        { ActivityType.WaterSports, HKWorkoutActivityType.WaterSports },
        { ActivityType.Wrestling, HKWorkoutActivityType.Wrestling },
        { ActivityType.Yoga, HKWorkoutActivityType.Yoga },
        { ActivityType.Barre, HKWorkoutActivityType.Barre },
        { ActivityType.CoreTraining, HKWorkoutActivityType.CoreTraining },
        { ActivityType.Skiing, HKWorkoutActivityType.DownhillSkiing }, // not 1:1 mapping
        { ActivityType.Stretching, HKWorkoutActivityType.Flexibility }, // not 1:1 mapping
        { ActivityType.HighIntensityIntervalTraining, HKWorkoutActivityType.HighIntensityIntervalTraining },
        { ActivityType.JumpRope, HKWorkoutActivityType.JumpRope },
        { ActivityType.Kickboxing, HKWorkoutActivityType.Kickboxing },
        { ActivityType.Pilates, HKWorkoutActivityType.Pilates },
        { ActivityType.Snowboarding, HKWorkoutActivityType.Snowboarding },
        { ActivityType.Stairs, HKWorkoutActivityType.Stairs },
        { ActivityType.StepTraining, HKWorkoutActivityType.StepTraining },
        { ActivityType.WheelchairWalkPace, HKWorkoutActivityType.WheelchairWalkPace },
        { ActivityType.WheelchairRunPace, HKWorkoutActivityType.WheelchairRunPace },
        { ActivityType.TaiChi, HKWorkoutActivityType.TaiChi },
        { ActivityType.MixedCardio, HKWorkoutActivityType.MixedCardio },
        { ActivityType.HandCycling, HKWorkoutActivityType.HandCycling },
        { ActivityType.DiscSports, HKWorkoutActivityType.DiscSports },
        { ActivityType.FitnessGaming, HKWorkoutActivityType.FitnessGaming },
        { ActivityType.Calisthenics, HKWorkoutActivityType.FunctionalStrengthTraining }, // not 1:1 mapping
        { ActivityType.Cooldown, HKWorkoutActivityType.Cooldown },
        { ActivityType.Pickleball, HKWorkoutActivityType.Pickleball },
        //{ ActivityType.SwimBikeRun, HKWorkoutActivityType.SwimBikeRun }, // iOS 16.0+
        //{ ActivityType.Transition, HKWorkoutActivityType.Transition }, // iOS 16.0+
        //{ ActivityType.UnderwaterDiving, HKWorkoutActivityType.UnderwaterDiving }, // iOS 17.0+
        { ActivityType.Unknown, HKWorkoutActivityType.Other }
    };

    internal static ActivityType ToActivityType(this HKWorkoutActivityType workoutActivityType)
    {
        var entry = ActivityTypeMap.FirstOrDefault(kvp => kvp.Value == workoutActivityType);
        if (!entry.Equals(default(KeyValuePair<ActivityType, HKWorkoutActivityType>)))
        {
            return entry.Key;
        }

        return ActivityType.Unknown;
    }

    internal static HKWorkoutActivityType ToHKWorkoutActivityType(this ActivityType activityType)
    {
        if (ActivityTypeMap.TryGetValue(activityType, out var workoutActivityType))
        {
            return workoutActivityType;
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
