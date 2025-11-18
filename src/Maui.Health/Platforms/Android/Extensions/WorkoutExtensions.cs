using AndroidX.Health.Connect.Client.Records;
using AndroidX.Health.Connect.Client.Records.Metadata;
using Java.Time;
using Maui.Health.Enums;
using Maui.Health.Models.Metrics;
using Maui.Health.Platforms.Android.Enums;
using System.Diagnostics;
using System.Linq;

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
    /// Converts an ExerciseSessionRecord to a WorkoutDto with heart rate data (async)
    /// </summary>
    public static async Task<WorkoutDto> ToWorkoutDtoAsync(
        this ExerciseSessionRecord exerciseRecord,
        Func<HealthTimeRange, CancellationToken, Task<HeartRateDto[]>> queryHeartRateFunc,
        CancellationToken cancellationToken)
    {
        var startTime = exerciseRecord.StartTime.ToDateTimeOffset();
        var endTime = exerciseRecord.EndTime.ToDateTimeOffset();
        var activityType = ((ExerciseType)exerciseRecord.ExerciseType).ToActivityType();
        string? title = exerciseRecord.Title;

        double? avgHeartRate = null;
        double? minHeartRate = null;
        double? maxHeartRate = null;

        try
        {
            Debug.WriteLine($"Android: Querying HR for workout {startTime:HH:mm} to {endTime:HH:mm}");
            var workoutTimeRange = HealthTimeRange.FromDateTimeOffset(startTime, endTime);
            var heartRateData = await queryHeartRateFunc(workoutTimeRange, cancellationToken);
            Debug.WriteLine($"Android: Found {heartRateData.Length} HR samples for workout");

            if (heartRateData.Any())
            {
                avgHeartRate = heartRateData.Average(hr => hr.BeatsPerMinute);
                minHeartRate = heartRateData.Min(hr => hr.BeatsPerMinute);
                maxHeartRate = heartRateData.Max(hr => hr.BeatsPerMinute);
                Debug.WriteLine($"Android: Workout HR - Avg: {avgHeartRate:F0}, Min: {minHeartRate:F0}, Max: {maxHeartRate:F0}");
            }
            else
            {
                Debug.WriteLine($"Android: No HR data found for workout period");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Android: Error fetching heart rate for workout: {ex.Message}");
            Debug.WriteLine($"Android: Stack trace: {ex.StackTrace}");
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
            MaxHeartRate = maxHeartRate
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
        var startTime = Instant.Parse(dto.StartTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss'Z'"));
        var endTime = Instant.Parse(dto.EndTime.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss'Z'"));
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
            ActivityType.Running => 7,
            ActivityType.Cycling => 8,
            ActivityType.Walking => 79,
            ActivityType.Swimming => 68,
            ActivityType.Hiking => 36,
            ActivityType.Yoga => 81,
            ActivityType.Calisthenics => 28,
            ActivityType.StrengthTraining => 71,
            ActivityType.Elliptical => 25,
            ActivityType.Rowing => 61,
            ActivityType.Pilates => 54,
            ActivityType.Dance => 19,
            ActivityType.Soccer => 62,
            ActivityType.Basketball => 9,
            ActivityType.Baseball => 5,
            ActivityType.Tennis => 73,
            ActivityType.Golf => 32,
            ActivityType.Badminton => 3,
            ActivityType.TableTennis => 72,
            ActivityType.Volleyball => 78,
            ActivityType.Cricket => 18,
            ActivityType.Rugby => 63,
            ActivityType.AmericanFootball => 1,
            ActivityType.Skiing => 64,
            ActivityType.Snowboarding => 66,
            ActivityType.SkatingSports => 40,
            ActivityType.SurfingSports => 67,
            ActivityType.PaddleSports => 53,
            ActivityType.Sailing => 65,
            ActivityType.MartialArts => 47,
            ActivityType.Boxing => 11,
            ActivityType.Wrestling => 82,
            ActivityType.Climbing => 59,
            ActivityType.CrossTraining => 20,
            ActivityType.StairClimbing => 70,
            ActivityType.JumpRope => 44,
            ActivityType.Unknown => 0,
            _ => 0
        };
    }
}
