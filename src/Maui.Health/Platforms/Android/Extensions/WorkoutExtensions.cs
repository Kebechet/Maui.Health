using AndroidX.Health.Connect.Client.Records;
using AndroidX.Health.Connect.Client.Records.Metadata;
using Java.Time;
using Maui.Health.Enums;
using Maui.Health.Models.Metrics;
using Maui.Health.Platforms.Android.Enums;

namespace Maui.Health.Platforms.Android.Extensions;

internal static class WorkoutExtensions
{
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
    /// Converts a WorkoutDto to an ExerciseSessionRecord for writing to Android Health Connect
    /// Note: EndTime must not be null (only for completed workouts)
    /// </summary>
    public static ExerciseSessionRecord? ToExerciseSessionRecord(this WorkoutDto dto)
    {
        // Can only write completed workouts (with EndTime) to Health Connect
        if (dto.EndTime is null)
        {
            return null;
        }

        var offset = ZoneOffset.SystemDefault().Rules!.GetOffset(Instant.Now());

        return new ExerciseSessionRecord(
            dto.StartTime.ToJavaInstant()!,
            offset,
            dto.EndTime.Value.ToJavaInstant()!,
            offset,
            Metadata.ManualEntry(),
            (int)dto.ActivityType.ToExerciseType(),
            !string.IsNullOrEmpty(dto.Title) ? dto.Title : null,
            null
        );
    }
}
