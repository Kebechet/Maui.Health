using Maui.Health.Enums;
using Maui.Health.Platforms.Android.Enums;

namespace Maui.Health.Platforms.Android.Extensions;

internal static class ExerciseTypeExtensions
{
    public static ActivityType ToActivityType(this ExerciseType exerciseType)
    {
        return exerciseType switch
        {
            ExerciseType.OtherWorkout => ActivityType.Unknown,//not 1:1 mapping
            ExerciseType.Badminton => ActivityType.Badminton,
            ExerciseType.Baseball => ActivityType.Baseball,
            ExerciseType.Basketball => ActivityType.Basketball,
            ExerciseType.Biking => ActivityType.Cycling,
            ExerciseType.BikingStationary => ActivityType.CyclingStationary,
            ExerciseType.BootCamp => ActivityType.BootCamp,
            ExerciseType.Boxing => ActivityType.Boxing,
            ExerciseType.Calisthenics => ActivityType.Calisthenics,
            ExerciseType.Cricket => ActivityType.Cricket,
            ExerciseType.Dancing => ActivityType.Dance,
            ExerciseType.Elliptical => ActivityType.Elliptical,
            ExerciseType.ExerciseClass => ActivityType.ExerciseClass,
            ExerciseType.Fencing => ActivityType.Fencing,
            ExerciseType.FootballAmerican => ActivityType.AmericanFootball,
            ExerciseType.FootballAustralian => ActivityType.AustralianFootball,
            ExerciseType.FrisbeeDisc => ActivityType.FrisbeeDisc,
            ExerciseType.Golf => ActivityType.Golf,
            ExerciseType.GuidedBreathing => ActivityType.MindAndBody,//not 1:1 mapping
            ExerciseType.Gymnastics => ActivityType.Gymnastics,
            ExerciseType.Handball => ActivityType.Handball,
            ExerciseType.HighIntensityIntervalTraining => ActivityType.HighIntensityIntervalTraining,
            ExerciseType.Hiking => ActivityType.Hiking,
            ExerciseType.IceHockey => ActivityType.Hockey,//not 1:1 mapping
            ExerciseType.IceSkating => ActivityType.SkatingSports,//not 1:1 mapping
            ExerciseType.MartialArts => ActivityType.MartialArts,
            ExerciseType.Paddling => ActivityType.PaddleSports,
            ExerciseType.Paragliding => ActivityType.Paragliding,
            ExerciseType.Pilates => ActivityType.Pilates,
            ExerciseType.Racquetball => ActivityType.Racquetball,
            ExerciseType.RockClimbing => ActivityType.Climbing,//not 1:1 mapping
            ExerciseType.RollerHockey => ActivityType.Hockey,
            ExerciseType.Rowing => ActivityType.Rowing,
            ExerciseType.RowingMachine => ActivityType.Rowing,//not 1:1 mapping
            ExerciseType.Rugby => ActivityType.Rugby,
            ExerciseType.Running => ActivityType.Running,
            ExerciseType.RunningTreadmill => ActivityType.Running,//not 1:1 mapping
            ExerciseType.Sailing => ActivityType.Sailing,
            ExerciseType.ScubaDiving => ActivityType.UnderwaterDiving,
            ExerciseType.Skating => ActivityType.SkatingSports,//not 1:1 mapping
            ExerciseType.Skiing => ActivityType.Skiing,
            ExerciseType.Snowboarding => ActivityType.Snowboarding,
            ExerciseType.Snowshoeing => ActivityType.SnowSports,
            ExerciseType.Soccer => ActivityType.Soccer,
            ExerciseType.Softball => ActivityType.Softball,
            ExerciseType.Squash => ActivityType.Squash,
            ExerciseType.StairClimbing => ActivityType.StairClimbing,
            ExerciseType.StairClimbingMachine => ActivityType.StairClimbing,//not 1:1 mapping
            ExerciseType.StrengthTraining => ActivityType.StrengthTraining,
            ExerciseType.Stretching => ActivityType.Stretching,
            ExerciseType.Surfing => ActivityType.SurfingSports,//not 1:1 mapping
            ExerciseType.SwimmingOpenWater => ActivityType.Swimming,//not 1:1 mapping
            ExerciseType.SwimmingPool => ActivityType.Swimming,//not 1:1 mapping
            ExerciseType.TableTennis => ActivityType.TableTennis,
            ExerciseType.Tennis => ActivityType.Tennis,
            ExerciseType.Volleyball => ActivityType.Volleyball,
            ExerciseType.Walking => ActivityType.Walking,
            ExerciseType.WaterPolo => ActivityType.WaterPolo,
            ExerciseType.Weightlifting => ActivityType.Weightlifting,
            ExerciseType.Wheelchair => ActivityType.WheelchairWalkPace,//not 1:1 mapping
            ExerciseType.Yoga => ActivityType.Yoga,
            _ => ActivityType.Unknown
        };
    }
}
