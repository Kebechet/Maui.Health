using HealthKit;
using Maui.Health.Constants;
using Maui.Health.Enums;
using Maui.Health.Models.Metrics;

namespace Maui.Health.Platforms.iOS.Extensions;

internal static class HKWorkoutActivityTypeExtensions
{
    /// <summary>
    /// Bidirectional mapping between HKWorkoutActivityType and ActivityType.
    /// Comments indicate where mappings are not 1:1.
    /// </summary>
    private static readonly Dictionary<HKWorkoutActivityType, ActivityType> HKWorkoutToActivityMap = new()
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

    /// <summary>
    /// Reverse mapping from ActivityType to HKWorkoutActivityType.
    /// For ActivityTypes that map to multiple HKWorkoutActivityTypes, we pick the most common one.
    /// </summary>
    private static readonly Dictionary<ActivityType, HKWorkoutActivityType> ActivityToHKWorkoutMap = new()
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
        { ActivityType.DanceInspiredTraining, HKWorkoutActivityType.DanceInspiredTraining },
        { ActivityType.Elliptical, HKWorkoutActivityType.Elliptical },
        { ActivityType.EquestrianSports, HKWorkoutActivityType.EquestrianSports },
        { ActivityType.Fencing, HKWorkoutActivityType.Fencing },
        { ActivityType.Fishing, HKWorkoutActivityType.Fishing },
        { ActivityType.Weightlifting, HKWorkoutActivityType.FunctionalStrengthTraining },
        { ActivityType.Golf, HKWorkoutActivityType.Golf },
        { ActivityType.Gymnastics, HKWorkoutActivityType.Gymnastics },
        { ActivityType.Handball, HKWorkoutActivityType.Handball },
        { ActivityType.Hiking, HKWorkoutActivityType.Hiking },
        { ActivityType.Hockey, HKWorkoutActivityType.Hockey },
        { ActivityType.Hunting, HKWorkoutActivityType.Hunting },
        { ActivityType.Lacrosse, HKWorkoutActivityType.Lacrosse },
        { ActivityType.MartialArts, HKWorkoutActivityType.MartialArts },
        { ActivityType.MindAndBody, HKWorkoutActivityType.MindAndBody },
        { ActivityType.MixedMetabolicCardioTraining, HKWorkoutActivityType.MixedMetabolicCardioTraining },
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
        { ActivityType.StrengthTraining, HKWorkoutActivityType.TraditionalStrengthTraining },
        { ActivityType.Volleyball, HKWorkoutActivityType.Volleyball },
        { ActivityType.Walking, HKWorkoutActivityType.Walking },
        { ActivityType.WaterFitness, HKWorkoutActivityType.WaterFitness },
        { ActivityType.WaterPolo, HKWorkoutActivityType.WaterPolo },
        { ActivityType.WaterSports, HKWorkoutActivityType.WaterSports },
        { ActivityType.Wrestling, HKWorkoutActivityType.Wrestling },
        { ActivityType.Yoga, HKWorkoutActivityType.Yoga },
        { ActivityType.Barre, HKWorkoutActivityType.Barre },
        { ActivityType.CoreTraining, HKWorkoutActivityType.CoreTraining },
        { ActivityType.Skiing, HKWorkoutActivityType.DownhillSkiing },
        { ActivityType.Stretching, HKWorkoutActivityType.Flexibility },
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
        { ActivityType.Pickleball, HKWorkoutActivityType.Pickleball },
        { ActivityType.Cooldown, HKWorkoutActivityType.Cooldown },
        { ActivityType.SwimBikeRun, HKWorkoutActivityType.SwimBikeRun },
        { ActivityType.Transition, HKWorkoutActivityType.Transition },
        { ActivityType.UnderwaterDiving, HKWorkoutActivityType.UnderwaterDiving },
        { ActivityType.Calisthenics, HKWorkoutActivityType.FunctionalStrengthTraining },
        { ActivityType.Unknown, HKWorkoutActivityType.Other }
    };

    internal static ActivityType ToActivityType(this HKWorkoutActivityType workoutActivityType)
    {
        if (HKWorkoutToActivityMap.TryGetValue(workoutActivityType, out var activityType))
        {
            return activityType;
        }

        return ActivityType.Unknown;
    }

    internal static HKWorkoutActivityType ToHKWorkoutActivityType(this ActivityType activityType)
    {
        if (ActivityToHKWorkoutMap.TryGetValue(activityType, out var workoutActivityType))
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
        if (workout.TotalEnergyBurned != null)
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
            DataOrigin = workout.SourceRevision?.Source?.Name ?? DataOrigins.Unknown,
            Timestamp = startTime,
            ActivityType = activityType,
            StartTime = startTime,
            EndTime = endTime,
            EnergyBurned = energyBurned,
            Distance = distance
        };
    }

    internal static async Task<WorkoutDto> ToWorkoutDtoAsync(
       this HKWorkout workout,
       Func<HealthTimeRange, CancellationToken, Task<HeartRateDto[]>> queryHeartRateFunc,
       CancellationToken cancellationToken)
    {
        var startTime = workout.StartDate.ToDateTimeOffset();
        var endTime = workout.EndDate.ToDateTimeOffset();
        var activityType = workout.WorkoutActivityType.ToActivityType();

        // Extract energy burned from HKWorkout (iOS has this built-in)
        double? energyBurned = null;
        if (workout.TotalEnergyBurned != null)
        {
            energyBurned = workout.TotalEnergyBurned.GetDoubleValue(HKUnit.Kilocalorie);
        }

        // Extract distance from HKWorkout (iOS has this built-in)
        double? distance = null;
        if (workout.TotalDistance != null)
        {
            distance = workout.TotalDistance.GetDoubleValue(HKUnit.Meter);
        }

        // Fetch heart rate data during the workout
        double? avgHeartRate = null;
        double? minHeartRate = null;
        double? maxHeartRate = null;

        try
        {
            var workoutTimeRange = HealthTimeRange.FromDateTimeOffset(startTime, endTime);
            var heartRateData = await queryHeartRateFunc(workoutTimeRange, cancellationToken);

            if (heartRateData.Any())
            {
                avgHeartRate = heartRateData.Average(hr => hr.BeatsPerMinute);
                minHeartRate = heartRateData.Min(hr => hr.BeatsPerMinute);
                maxHeartRate = heartRateData.Max(hr => hr.BeatsPerMinute);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"iOS: Error fetching heart rate for workout: {ex.Message}");
        }

        return new WorkoutDto
        {
            Id = workout.Uuid.ToString(),
            DataOrigin = workout.SourceRevision?.Source?.Name ?? DataOrigins.Unknown,
            Timestamp = startTime,
            ActivityType = activityType,
            StartTime = startTime,
            EndTime = endTime,
            EnergyBurned = energyBurned,
            Distance = distance,
            AverageHeartRate = avgHeartRate,
            MinHeartRate = minHeartRate,
            MaxHeartRate = maxHeartRate
        };
    }
}
