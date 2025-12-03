using Maui.Health.Enums;
using Maui.Health.Platforms.Android.Enums;

namespace Maui.Health.Platforms.Android.Extensions;

internal static class ExerciseTypeExtensions
{
    /// <summary>
    /// Bidirectional mapping between ActivityType (key) and ExerciseType (value).
    /// To convert ActivityType -> ExerciseType: lookup by key.
    /// To convert ExerciseType -> ActivityType: search by value.
    /// Comments indicate where mappings are not 1:1.
    /// </summary>
    private static readonly Dictionary<ActivityType, ExerciseType> ActivityTypeMap = new()
    {
        { ActivityType.Unknown, ExerciseType.OtherWorkout }, // not 1:1 mapping
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
        { ActivityType.MindAndBody, ExerciseType.GuidedBreathing }, // not 1:1 mapping
        { ActivityType.Gymnastics, ExerciseType.Gymnastics },
        { ActivityType.Handball, ExerciseType.Handball },
        { ActivityType.HighIntensityIntervalTraining, ExerciseType.HighIntensityIntervalTraining },
        { ActivityType.Hiking, ExerciseType.Hiking },
        { ActivityType.Hockey, ExerciseType.IceHockey }, // not 1:1 mapping
        { ActivityType.SkatingSports, ExerciseType.Skating }, // not 1:1 mapping
        { ActivityType.MartialArts, ExerciseType.MartialArts },
        { ActivityType.PaddleSports, ExerciseType.Paddling },
        { ActivityType.Paragliding, ExerciseType.Paragliding },
        { ActivityType.Pilates, ExerciseType.Pilates },
        { ActivityType.Racquetball, ExerciseType.Racquetball },
        { ActivityType.Climbing, ExerciseType.RockClimbing }, // not 1:1 mapping
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
        { ActivityType.SurfingSports, ExerciseType.Surfing }, // not 1:1 mapping
        { ActivityType.Swimming, ExerciseType.SwimmingPool }, // not 1:1 mapping
        { ActivityType.TableTennis, ExerciseType.TableTennis },
        { ActivityType.Tennis, ExerciseType.Tennis },
        { ActivityType.Volleyball, ExerciseType.Volleyball },
        { ActivityType.Walking, ExerciseType.Walking },
        { ActivityType.WaterPolo, ExerciseType.WaterPolo },
        { ActivityType.Weightlifting, ExerciseType.Weightlifting },
        { ActivityType.WheelchairWalkPace, ExerciseType.Wheelchair }, // not 1:1 mapping
        { ActivityType.Yoga, ExerciseType.Yoga }
    };

    public static ActivityType ToActivityType(this ExerciseType exerciseType)
    {
        var entry = ActivityTypeMap.FirstOrDefault(kvp => kvp.Value == exerciseType);
        if (!entry.Equals(default(KeyValuePair<ActivityType, ExerciseType>)))
        {
            return entry.Key;
        }

        return ActivityType.Unknown;
    }

    public static ExerciseType ToExerciseType(this ActivityType activityType)
    {
        if (ActivityTypeMap.TryGetValue(activityType, out var exerciseType))
        {
            return exerciseType;
        }

        return ExerciseType.OtherWorkout;
    }
}
