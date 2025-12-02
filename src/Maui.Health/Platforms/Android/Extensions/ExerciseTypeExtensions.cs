using Maui.Health.Enums;
using Maui.Health.Platforms.Android.Enums;

namespace Maui.Health.Platforms.Android.Extensions;

internal static class ExerciseTypeExtensions
{
    /// <summary>
    /// Bidirectional mapping between ExerciseType and ActivityType.
    /// Comments indicate where mappings are not 1:1.
    /// </summary>
    private static readonly Dictionary<ExerciseType, ActivityType> ExerciseToActivityMap = new()
    {
        { ExerciseType.OtherWorkout, ActivityType.Unknown }, // not 1:1 mapping
        { ExerciseType.Badminton, ActivityType.Badminton },
        { ExerciseType.Baseball, ActivityType.Baseball },
        { ExerciseType.Basketball, ActivityType.Basketball },
        { ExerciseType.Biking, ActivityType.Cycling },
        { ExerciseType.BikingStationary, ActivityType.CyclingStationary },
        { ExerciseType.BootCamp, ActivityType.BootCamp },
        { ExerciseType.Boxing, ActivityType.Boxing },
        { ExerciseType.Calisthenics, ActivityType.Calisthenics },
        { ExerciseType.Cricket, ActivityType.Cricket },
        { ExerciseType.Dancing, ActivityType.Dance },
        { ExerciseType.Elliptical, ActivityType.Elliptical },
        { ExerciseType.ExerciseClass, ActivityType.ExerciseClass },
        { ExerciseType.Fencing, ActivityType.Fencing },
        { ExerciseType.FootballAmerican, ActivityType.AmericanFootball },
        { ExerciseType.FootballAustralian, ActivityType.AustralianFootball },
        { ExerciseType.FrisbeeDisc, ActivityType.FrisbeeDisc },
        { ExerciseType.Golf, ActivityType.Golf },
        { ExerciseType.GuidedBreathing, ActivityType.MindAndBody }, // not 1:1 mapping
        { ExerciseType.Gymnastics, ActivityType.Gymnastics },
        { ExerciseType.Handball, ActivityType.Handball },
        { ExerciseType.HighIntensityIntervalTraining, ActivityType.HighIntensityIntervalTraining },
        { ExerciseType.Hiking, ActivityType.Hiking },
        { ExerciseType.IceHockey, ActivityType.Hockey }, // not 1:1 mapping
        { ExerciseType.IceSkating, ActivityType.SkatingSports }, // not 1:1 mapping
        { ExerciseType.MartialArts, ActivityType.MartialArts },
        { ExerciseType.Paddling, ActivityType.PaddleSports },
        { ExerciseType.Paragliding, ActivityType.Paragliding },
        { ExerciseType.Pilates, ActivityType.Pilates },
        { ExerciseType.Racquetball, ActivityType.Racquetball },
        { ExerciseType.RockClimbing, ActivityType.Climbing }, // not 1:1 mapping
        { ExerciseType.RollerHockey, ActivityType.Hockey },
        { ExerciseType.Rowing, ActivityType.Rowing },
        { ExerciseType.RowingMachine, ActivityType.Rowing }, // not 1:1 mapping
        { ExerciseType.Rugby, ActivityType.Rugby },
        { ExerciseType.Running, ActivityType.Running },
        { ExerciseType.RunningTreadmill, ActivityType.Running }, // not 1:1 mapping
        { ExerciseType.Sailing, ActivityType.Sailing },
        { ExerciseType.ScubaDiving, ActivityType.UnderwaterDiving },
        { ExerciseType.Skating, ActivityType.SkatingSports }, // not 1:1 mapping
        { ExerciseType.Skiing, ActivityType.Skiing },
        { ExerciseType.Snowboarding, ActivityType.Snowboarding },
        { ExerciseType.Snowshoeing, ActivityType.SnowSports },
        { ExerciseType.Soccer, ActivityType.Soccer },
        { ExerciseType.Softball, ActivityType.Softball },
        { ExerciseType.Squash, ActivityType.Squash },
        { ExerciseType.StairClimbing, ActivityType.StairClimbing },
        { ExerciseType.StairClimbingMachine, ActivityType.StairClimbing }, // not 1:1 mapping
        { ExerciseType.StrengthTraining, ActivityType.StrengthTraining },
        { ExerciseType.Stretching, ActivityType.Stretching },
        { ExerciseType.Surfing, ActivityType.SurfingSports }, // not 1:1 mapping
        { ExerciseType.SwimmingOpenWater, ActivityType.Swimming }, // not 1:1 mapping
        { ExerciseType.SwimmingPool, ActivityType.Swimming }, // not 1:1 mapping
        { ExerciseType.TableTennis, ActivityType.TableTennis },
        { ExerciseType.Tennis, ActivityType.Tennis },
        { ExerciseType.Volleyball, ActivityType.Volleyball },
        { ExerciseType.Walking, ActivityType.Walking },
        { ExerciseType.WaterPolo, ActivityType.WaterPolo },
        { ExerciseType.Weightlifting, ActivityType.Weightlifting },
        { ExerciseType.Wheelchair, ActivityType.WheelchairWalkPace }, // not 1:1 mapping
        { ExerciseType.Yoga, ActivityType.Yoga }
    };

    /// <summary>
    /// Reverse mapping from ActivityType to ExerciseType.
    /// For ActivityTypes that map to multiple ExerciseTypes, we pick the most common one.
    /// </summary>
    private static readonly Dictionary<ActivityType, ExerciseType> ActivityToExerciseMap = new()
    {
        { ActivityType.Unknown, ExerciseType.OtherWorkout },
        { ActivityType.Badminton, ExerciseType.Badminton },
        { ActivityType.Baseball, ExerciseType.Baseball },
        { ActivityType.Basketball, ExerciseType.Basketball },
        { ActivityType.Cycling, ExerciseType.Biking },
        { ActivityType.CyclingStationary, ExerciseType.BikingStationary },
        { ActivityType.BootCamp, ExerciseType.BootCamp },
        { ActivityType.Boxing, ExerciseType.Boxing },
        { ActivityType.Calisthenics, ExerciseType.Calisthenics },
        { ActivityType.Cricket, ExerciseType.Cricket },
        { ActivityType.Dance, ExerciseType.Dancing },
        { ActivityType.Elliptical, ExerciseType.Elliptical },
        { ActivityType.ExerciseClass, ExerciseType.ExerciseClass },
        { ActivityType.Fencing, ExerciseType.Fencing },
        { ActivityType.AmericanFootball, ExerciseType.FootballAmerican },
        { ActivityType.AustralianFootball, ExerciseType.FootballAustralian },
        { ActivityType.FrisbeeDisc, ExerciseType.FrisbeeDisc },
        { ActivityType.Golf, ExerciseType.Golf },
        { ActivityType.MindAndBody, ExerciseType.GuidedBreathing },
        { ActivityType.Gymnastics, ExerciseType.Gymnastics },
        { ActivityType.Handball, ExerciseType.Handball },
        { ActivityType.HighIntensityIntervalTraining, ExerciseType.HighIntensityIntervalTraining },
        { ActivityType.Hiking, ExerciseType.Hiking },
        { ActivityType.Hockey, ExerciseType.IceHockey },
        { ActivityType.SkatingSports, ExerciseType.Skating },
        { ActivityType.MartialArts, ExerciseType.MartialArts },
        { ActivityType.PaddleSports, ExerciseType.Paddling },
        { ActivityType.Paragliding, ExerciseType.Paragliding },
        { ActivityType.Pilates, ExerciseType.Pilates },
        { ActivityType.Racquetball, ExerciseType.Racquetball },
        { ActivityType.Climbing, ExerciseType.RockClimbing },
        { ActivityType.Rowing, ExerciseType.Rowing },
        { ActivityType.Rugby, ExerciseType.Rugby },
        { ActivityType.Running, ExerciseType.Running },
        { ActivityType.Sailing, ExerciseType.Sailing },
        { ActivityType.UnderwaterDiving, ExerciseType.ScubaDiving },
        { ActivityType.Skiing, ExerciseType.Skiing },
        { ActivityType.Snowboarding, ExerciseType.Snowboarding },
        { ActivityType.SnowSports, ExerciseType.Snowshoeing },
        { ActivityType.Soccer, ExerciseType.Soccer },
        { ActivityType.Softball, ExerciseType.Softball },
        { ActivityType.Squash, ExerciseType.Squash },
        { ActivityType.StairClimbing, ExerciseType.StairClimbing },
        { ActivityType.StrengthTraining, ExerciseType.StrengthTraining },
        { ActivityType.Stretching, ExerciseType.Stretching },
        { ActivityType.SurfingSports, ExerciseType.Surfing },
        { ActivityType.Swimming, ExerciseType.SwimmingPool },
        { ActivityType.TableTennis, ExerciseType.TableTennis },
        { ActivityType.Tennis, ExerciseType.Tennis },
        { ActivityType.Volleyball, ExerciseType.Volleyball },
        { ActivityType.Walking, ExerciseType.Walking },
        { ActivityType.WaterPolo, ExerciseType.WaterPolo },
        { ActivityType.Weightlifting, ExerciseType.Weightlifting },
        { ActivityType.WheelchairWalkPace, ExerciseType.Wheelchair },
        { ActivityType.Yoga, ExerciseType.Yoga }
    };

    public static ActivityType ToActivityType(this ExerciseType exerciseType)
    {
        if (ExerciseToActivityMap.TryGetValue(exerciseType, out var activityType))
        {
            return activityType;
        }

        return ActivityType.Unknown;
    }

    public static ExerciseType ToExerciseType(this ActivityType activityType)
    {
        if (ActivityToExerciseMap.TryGetValue(activityType, out var exerciseType))
        {
            return exerciseType;
        }

        return ExerciseType.OtherWorkout;
    }
}
