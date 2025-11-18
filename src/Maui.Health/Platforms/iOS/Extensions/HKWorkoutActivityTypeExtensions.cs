using HealthKit;
using Maui.Health.Enums;
using Maui.Health.Models.Metrics;

namespace Maui.Health.Platforms.iOS.Extensions;

internal static class HKWorkoutActivityTypeExtensions
{
    internal static ActivityType ToActivityType(this HKWorkoutActivityType workoutActivityType)
    {
        return workoutActivityType switch
        {
            HKWorkoutActivityType.AmericanFootball => ActivityType.AmericanFootball,
            HKWorkoutActivityType.Archery => ActivityType.Archery,
            HKWorkoutActivityType.AustralianFootball => ActivityType.AustralianFootball,
            HKWorkoutActivityType.Badminton => ActivityType.Badminton,
            HKWorkoutActivityType.Baseball => ActivityType.Baseball,
            HKWorkoutActivityType.Basketball => ActivityType.Basketball,
            HKWorkoutActivityType.Bowling => ActivityType.Bowling,
            HKWorkoutActivityType.Boxing => ActivityType.Boxing,
            HKWorkoutActivityType.Climbing => ActivityType.Climbing,
            HKWorkoutActivityType.Cricket => ActivityType.Cricket,
            HKWorkoutActivityType.CrossTraining => ActivityType.CrossTraining,
            HKWorkoutActivityType.Curling => ActivityType.Curling,
            HKWorkoutActivityType.Cycling => ActivityType.Cycling,
            HKWorkoutActivityType.Dance => ActivityType.Dance,
            HKWorkoutActivityType.DanceInspiredTraining => ActivityType.DanceInspiredTraining,
            HKWorkoutActivityType.Elliptical => ActivityType.Elliptical,
            HKWorkoutActivityType.EquestrianSports => ActivityType.EquestrianSports,
            HKWorkoutActivityType.Fencing => ActivityType.Fencing,
            HKWorkoutActivityType.Fishing => ActivityType.Fishing,
            HKWorkoutActivityType.FunctionalStrengthTraining => ActivityType.Weightlifting,//not 1:1 mapping
            HKWorkoutActivityType.Golf => ActivityType.Golf,
            HKWorkoutActivityType.Gymnastics => ActivityType.Gymnastics,
            HKWorkoutActivityType.Handball => ActivityType.Handball,
            HKWorkoutActivityType.Hiking => ActivityType.Hiking,
            HKWorkoutActivityType.Hockey => ActivityType.Hockey,
            HKWorkoutActivityType.Hunting => ActivityType.Hunting,
            HKWorkoutActivityType.Lacrosse => ActivityType.Lacrosse,
            HKWorkoutActivityType.MartialArts => ActivityType.MartialArts,
            HKWorkoutActivityType.MindAndBody => ActivityType.MindAndBody,
            HKWorkoutActivityType.MixedMetabolicCardioTraining => ActivityType.MixedMetabolicCardioTraining,
            HKWorkoutActivityType.PaddleSports => ActivityType.PaddleSports,
            HKWorkoutActivityType.Play => ActivityType.Play,
            HKWorkoutActivityType.PreparationAndRecovery => ActivityType.PreparationAndRecovery,
            HKWorkoutActivityType.Racquetball => ActivityType.Racquetball,
            HKWorkoutActivityType.Rowing => ActivityType.Rowing,
            HKWorkoutActivityType.Rugby => ActivityType.Rugby,
            HKWorkoutActivityType.Running => ActivityType.Running,
            HKWorkoutActivityType.Sailing => ActivityType.Sailing,
            HKWorkoutActivityType.SkatingSports => ActivityType.SkatingSports,
            HKWorkoutActivityType.SnowSports => ActivityType.SnowSports,
            HKWorkoutActivityType.Soccer => ActivityType.Soccer,
            HKWorkoutActivityType.Softball => ActivityType.Softball,
            HKWorkoutActivityType.Squash => ActivityType.Squash,
            HKWorkoutActivityType.StairClimbing => ActivityType.StairClimbing,
            HKWorkoutActivityType.SurfingSports => ActivityType.SurfingSports,
            HKWorkoutActivityType.Swimming => ActivityType.Swimming,
            HKWorkoutActivityType.TableTennis => ActivityType.TableTennis,
            HKWorkoutActivityType.Tennis => ActivityType.Tennis,
            HKWorkoutActivityType.TrackAndField => ActivityType.TrackAndField,
            HKWorkoutActivityType.TraditionalStrengthTraining => ActivityType.StrengthTraining,//not 1:1 mapping
            HKWorkoutActivityType.Volleyball => ActivityType.Volleyball,
            HKWorkoutActivityType.Walking => ActivityType.Walking,
            HKWorkoutActivityType.WaterFitness => ActivityType.WaterFitness,
            HKWorkoutActivityType.WaterPolo => ActivityType.WaterPolo,
            HKWorkoutActivityType.WaterSports => ActivityType.WaterSports,
            HKWorkoutActivityType.Wrestling => ActivityType.Wrestling,
            HKWorkoutActivityType.Yoga => ActivityType.Yoga,
            HKWorkoutActivityType.Barre => ActivityType.Barre,
            HKWorkoutActivityType.CoreTraining => ActivityType.CoreTraining,
            HKWorkoutActivityType.CrossCountrySkiing => ActivityType.Skiing,//not 1:1 mapping
            HKWorkoutActivityType.DownhillSkiing => ActivityType.Skiing,//not 1:1 mapping
            HKWorkoutActivityType.Flexibility => ActivityType.Stretching,//not 1:1 mapping
            HKWorkoutActivityType.HighIntensityIntervalTraining => ActivityType.HighIntensityIntervalTraining,
            HKWorkoutActivityType.JumpRope => ActivityType.JumpRope,
            HKWorkoutActivityType.Kickboxing => ActivityType.Kickboxing,
            HKWorkoutActivityType.Pilates => ActivityType.Pilates,
            HKWorkoutActivityType.Snowboarding => ActivityType.Snowboarding,
            HKWorkoutActivityType.Stairs => ActivityType.Stairs,
            HKWorkoutActivityType.StepTraining => ActivityType.StepTraining,
            HKWorkoutActivityType.WheelchairWalkPace => ActivityType.WheelchairWalkPace,
            HKWorkoutActivityType.WheelchairRunPace => ActivityType.WheelchairRunPace,
            HKWorkoutActivityType.TaiChi => ActivityType.TaiChi,
            HKWorkoutActivityType.MixedCardio => ActivityType.MixedCardio,
            HKWorkoutActivityType.HandCycling => ActivityType.HandCycling,
            HKWorkoutActivityType.DiscSports => ActivityType.DiscSports,
            HKWorkoutActivityType.FitnessGaming => ActivityType.FitnessGaming,
            HKWorkoutActivityType.CardioDance => ActivityType.Dance,//not 1:1 mapping
            HKWorkoutActivityType.SocialDance => ActivityType.Dance,//not 1:1 mapping
            HKWorkoutActivityType.Pickleball => ActivityType.Pickleball,
            HKWorkoutActivityType.Cooldown => ActivityType.Cooldown,
            HKWorkoutActivityType.SwimBikeRun => ActivityType.SwimBikeRun,
            HKWorkoutActivityType.Transition => ActivityType.Transition,
            HKWorkoutActivityType.UnderwaterDiving => ActivityType.UnderwaterDiving,
            HKWorkoutActivityType.Other => ActivityType.Unknown,
            _ => ActivityType.Unknown
        };
    }

    internal static async Task<WorkoutDto> ToWorkoutDtoAsync(
       this HKWorkout workout,
       Func<HealthTimeRange, CancellationToken, Task<HeartRateDto[]>> queryHeartRateFunc,
       CancellationToken cancellationToken)
    {
        // Use direct NSDate to DateTimeOffset conversion for proper UTC handling
        var startTime = workout.StartDate.ToDateTimeOffset();
        var endTime = workout.EndDate.ToDateTimeOffset();
        var activityType = workout.WorkoutActivityType.ToActivityType();

        // Extract energy burned
        double? energyBurned = null;
        if (workout.TotalEnergyBurned != null)
        {
            energyBurned = workout.TotalEnergyBurned.GetDoubleValue(HKUnit.Kilocalorie);
        }

        // Extract distance
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
            System.Diagnostics.Debug.WriteLine($"iOS: Querying HR for workout {startTime:HH:mm} to {endTime:HH:mm}");
            var workoutTimeRange = HealthTimeRange.FromDateTimeOffset(startTime, endTime);
            var heartRateData = await queryHeartRateFunc(workoutTimeRange, cancellationToken);
            System.Diagnostics.Debug.WriteLine($"iOS: Found {heartRateData.Length} HR samples for workout");

            if (heartRateData.Any())
            {
                avgHeartRate = heartRateData.Average(hr => hr.BeatsPerMinute);
                minHeartRate = heartRateData.Min(hr => hr.BeatsPerMinute);
                maxHeartRate = heartRateData.Max(hr => hr.BeatsPerMinute);
                System.Diagnostics.Debug.WriteLine($"iOS: Workout HR - Avg: {avgHeartRate:F0}, Min: {minHeartRate:F0}, Max: {maxHeartRate:F0}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"iOS: No HR data found for workout period");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"iOS: Error fetching heart rate for workout: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"iOS: Stack trace: {ex.StackTrace}");
        }

        return new WorkoutDto
        {
            Id = workout.Uuid.ToString(),
            DataOrigin = workout.SourceRevision?.Source?.Name ?? "Unknown",
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
