using AndroidX.Health.Connect.Client.Records;
using AndroidX.Health.Connect.Client.Records.Metadata;
using Java.Time;
using Maui.Health.Enums;
using Maui.Health.Models.Metrics;
using Maui.Health.Platforms.Android.Enums;
using System.Diagnostics;
using System.Linq;
using static Maui.Health.HealthConstants;

namespace Maui.Health.Platforms.Android.Extensions;

internal static class WorkoutExtensions
{
    /// <summary>
    /// Converts a WorkoutDto to a Java.Lang.Object (ExerciseSessionRecord) for writing to Android Health Connect
    /// </summary>
    public static Java.Lang.Object? ToAndroidRecord(this WorkoutDto workoutDto)
    {
        return workoutDto.ToExerciseSessionRecord();
    }

    /// <summary>
    /// Converts a Java.Lang.Object to a WorkoutDto (simple conversion without heart rate data)
    /// </summary>
    public static WorkoutDto? ToWorkoutDto(this Java.Lang.Object record)
    {
        if (record is not ExerciseSessionRecord exerciseRecord)
        {
            return null;
        }

        var startTime = exerciseRecord.StartTime.ToDateTimeOffset();
        var endTime = exerciseRecord.EndTime.ToDateTimeOffset();
        var activityType = ((ExerciseType)exerciseRecord.ExerciseType).ToActivityType();
        string? title = exerciseRecord.Title;

        return new WorkoutDto
        {
            Id = exerciseRecord.Metadata.Id,
            DataOrigin = exerciseRecord.Metadata.DataOrigin.PackageName,
            Timestamp = startTime,
            ActivityType = activityType,
            Title = title,
            StartTime = startTime,
            EndTime = endTime
        };
    }

    /// <summary>
    /// Converts an ExerciseSessionRecord to a WorkoutDto with heart rate and calories data (async)
    /// </summary>
    public static async Task<WorkoutDto> ToWorkoutDtoAsync(
        this ExerciseSessionRecord exerciseRecord,
        Func<HealthTimeRange, CancellationToken, Task<HeartRateDto[]>>? queryHeartRateFunc,
        Func<HealthTimeRange, CancellationToken, Task<ActiveCaloriesBurnedDto[]>>? queryCaloriesFunc,
        CancellationToken cancellationToken)
    {
        var startTime = exerciseRecord.StartTime.ToDateTimeOffset();
        var endTime = exerciseRecord.EndTime.ToDateTimeOffset();
        var activityType = ((ExerciseType)exerciseRecord.ExerciseType).ToActivityType();
        string? title = exerciseRecord.Title;
        var workoutTimeRange = HealthTimeRange.FromDateTimeOffset(startTime, endTime);

        double? avgHeartRate = null;
        double? minHeartRate = null;
        double? maxHeartRate = null;
        double? energyBurned = null;

        // Query heart rate
        if (queryHeartRateFunc != null)
        {
            try
            {
                Debug.WriteLine($"Android: Querying HR for workout {startTime:HH:mm} to {endTime:HH:mm}");
                var heartRateData = await queryHeartRateFunc(workoutTimeRange, cancellationToken);
                Debug.WriteLine($"Android: Found {heartRateData.Length} HR samples for workout");

                if (heartRateData.Any())
                {
                    avgHeartRate = heartRateData.Average(hr => hr.BeatsPerMinute);
                    minHeartRate = heartRateData.Min(hr => hr.BeatsPerMinute);
                    maxHeartRate = heartRateData.Max(hr => hr.BeatsPerMinute);
                    Debug.WriteLine($"Android: Workout HR - Avg: {avgHeartRate:F0}, Min: {minHeartRate:F0}, Max: {maxHeartRate:F0}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Android: Error fetching heart rate for workout: {ex.Message}");
            }
        }

        // Query calories
        if (queryCaloriesFunc != null)
        {
            try
            {
                Debug.WriteLine($"Android: Querying calories for workout {startTime:HH:mm} to {endTime:HH:mm}");
                var caloriesData = await queryCaloriesFunc(workoutTimeRange, cancellationToken);
                Debug.WriteLine($"Android: Found {caloriesData.Length} calorie samples for workout");

                if (caloriesData.Any())
                {
                    energyBurned = caloriesData.Sum(c => c.Energy);
                    Debug.WriteLine($"Android: Workout calories: {energyBurned:F0} kcal");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Android: Error fetching calories for workout: {ex.Message}");
            }
        }

        return new WorkoutDto
        {
            Id = exerciseRecord.Metadata.Id,
            DataOrigin = exerciseRecord.Metadata.DataOrigin.PackageName,
            Timestamp = startTime,
            ActivityType = activityType,
            Title = title,
            StartTime = startTime,
            EndTime = endTime,
            AverageHeartRate = avgHeartRate,
            MinHeartRate = minHeartRate,
            MaxHeartRate = maxHeartRate,
            EnergyBurned = energyBurned
        };
    }

    /// <summary>
    /// Converts a WorkoutDto to an ExerciseSessionRecord for writing to Android Health Connect
    /// Note: EndTime must not be null (only for completed workouts)
    /// </summary>
    public static ExerciseSessionRecord? ToExerciseSessionRecord(this WorkoutDto dto)
    {
        // Can only write completed workouts (with EndTime) to Health Connect
        if (!dto.EndTime.HasValue)
            return null;

#pragma warning disable CA1416
        var startTime = Instant.Parse(dto.StartTime.ToUniversalTime().ToString(DateFormats.Iso8601Utc));
        var endTime = Instant.Parse(dto.EndTime.Value.ToUniversalTime().ToString(DateFormats.Iso8601Utc));
#pragma warning restore CA1416

        var metadata = new Metadata();
        var offset = ZoneOffset.SystemDefault().Rules!.GetOffset(Instant.Now());

        var exerciseType = dto.ActivityType.ToAndroidExerciseType();

        var record = new ExerciseSessionRecord(
            startTime!,
            offset,
            endTime!,
            offset,
            exerciseType,
            !string.IsNullOrEmpty(dto.Title) ? dto.Title : null,
            null,
            metadata
        );

        return record;
    }

    /// <summary>
    /// Converts an ActivityType enum to Android Health Connect ExerciseType integer
    /// </summary>
    public static int ToAndroidExerciseType(this ActivityType activityType)
    {
        return activityType switch
        {
            // Use the correct ExerciseType enum values from Android Health Connect
            ActivityType.Running => (int)ExerciseType.Running, // 56
            ActivityType.Cycling => (int)ExerciseType.Biking, // 8
            ActivityType.CyclingStationary => (int)ExerciseType.BikingStationary, // 9
            ActivityType.Walking => (int)ExerciseType.Walking, // 79
            ActivityType.Swimming => (int)ExerciseType.SwimmingPool, // 74
            ActivityType.Hiking => (int)ExerciseType.Hiking, // 37
            ActivityType.Yoga => (int)ExerciseType.Yoga, // 83
            ActivityType.Calisthenics => (int)ExerciseType.Calisthenics, // 13
            ActivityType.StrengthTraining => (int)ExerciseType.StrengthTraining, // 70
            ActivityType.Elliptical => (int)ExerciseType.Elliptical, // 25
            ActivityType.Rowing => (int)ExerciseType.Rowing, // 53
            ActivityType.Pilates => (int)ExerciseType.Pilates, // 48
            ActivityType.Dance => (int)ExerciseType.Dancing, // 16
            ActivityType.Soccer => (int)ExerciseType.Soccer, // 64
            ActivityType.Basketball => (int)ExerciseType.Basketball, // 5
            ActivityType.Baseball => (int)ExerciseType.Baseball, // 4
            ActivityType.Tennis => (int)ExerciseType.Tennis, // 76
            ActivityType.Golf => (int)ExerciseType.Golf, // 32
            ActivityType.Badminton => (int)ExerciseType.Badminton, // 2
            ActivityType.TableTennis => (int)ExerciseType.TableTennis, // 75
            ActivityType.Volleyball => (int)ExerciseType.Volleyball, // 78
            ActivityType.Cricket => (int)ExerciseType.Cricket, // 14
            ActivityType.Rugby => (int)ExerciseType.Rugby, // 55
            ActivityType.AmericanFootball => (int)ExerciseType.FootballAmerican, // 28
            ActivityType.Skiing => (int)ExerciseType.Skiing, // 61
            ActivityType.Snowboarding => (int)ExerciseType.Snowboarding, // 62
            ActivityType.SkatingSports => (int)ExerciseType.Skating, // 60
            ActivityType.SurfingSports => (int)ExerciseType.Surfing, // 72
            ActivityType.PaddleSports => (int)ExerciseType.Paddling, // 46
            ActivityType.Sailing => (int)ExerciseType.Sailing, // 58
            ActivityType.MartialArts => (int)ExerciseType.MartialArts, // 44
            ActivityType.Boxing => (int)ExerciseType.Boxing, // 11
            ActivityType.Climbing => (int)ExerciseType.RockClimbing, // 51
            ActivityType.StairClimbing => (int)ExerciseType.StairClimbing, // 68
            ActivityType.BootCamp => (int)ExerciseType.BootCamp, // 10
            ActivityType.ExerciseClass => (int)ExerciseType.ExerciseClass, // 26
            ActivityType.HighIntensityIntervalTraining => (int)ExerciseType.HighIntensityIntervalTraining, // 36
            ActivityType.Stretching => (int)ExerciseType.Stretching, // 71
            ActivityType.Weightlifting => (int)ExerciseType.Weightlifting, // 81
            ActivityType.Unknown => (int)ExerciseType.OtherWorkout, // 0
            _ => (int)ExerciseType.OtherWorkout // 0
        };
    }
}
