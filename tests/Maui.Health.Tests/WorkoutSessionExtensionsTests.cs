using Maui.Health.Constants;
using Maui.Health.Enums;
using Maui.Health.Extensions;
using Maui.Health.Models;
using Maui.Health.Models.Metrics;
using Xunit;

namespace Maui.Health.Tests;

public class WorkoutSessionExtensionsTests
{
    private static WorkoutSession CreateSession(List<DateRange>? pauseIntervals = null)
    {
        return new WorkoutSession(
            "session-1",
            ActivityType.Running,
            "Morning Run",
            "com.test",
            DateTimeOffset.UtcNow.AddHours(-1),
            WorkoutSessionState.Running,
            pauseIntervals
        );
    }

    [Fact]
    public void ToWorkoutDto_BasicSession_MapsCorrectly()
    {
        // Arrange
        var session = CreateSession();
        var endTime = DateTimeOffset.UtcNow;

        // Act
        var dto = session.ToWorkoutDto(HealthDataSdk.GoogleHealthConnect, endTime, energyBurned: 500, distance: 5000);

        // Assert
        Assert.Equal("session-1", dto.Id);
        Assert.Equal("com.test", dto.DataOrigin);
        Assert.Equal(ActivityType.Running, dto.ActivityType);
        Assert.Equal("Morning Run", dto.Title);
        Assert.Equal(session.StartTime, dto.StartTime);
        Assert.Equal(endTime, dto.EndTime);
        Assert.Equal(500, dto.EnergyBurned);
        Assert.Equal(5000, dto.Distance);
    }

    [Fact]
    public void ToWorkoutDto_WithPauses_CalculatesActiveDuration()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var pauseIntervals = new List<DateRange>
        {
            new(now.AddMinutes(-40), now.AddMinutes(-30)),
        };
        var session = new WorkoutSession(
            "session-1", ActivityType.Cycling, null, "com.test",
            now.AddMinutes(-60),
            WorkoutSessionState.Running,
            pauseIntervals
        );

        // Act
        var dto = session.ToWorkoutDto(HealthDataSdk.GoogleHealthConnect, now);

        // Assert
        var metadata = dto.Metadata!;
        var activeDuration = (double)metadata[WorkoutMetadata.ActiveDurationSeconds];
        var pausedDuration = (double)metadata[WorkoutMetadata.PausedDurationSeconds];
        Assert.Equal(600, pausedDuration, precision: 1);
        Assert.Equal(3000, activeDuration, precision: 1);
        Assert.Equal(1, (int)metadata[WorkoutMetadata.PauseCount]);
    }

    [Fact]
    public void ToWorkoutDto_MetadataContainsPauseIntervals()
    {
        // Arrange
        var session = CreateSession();

        // Act
        var dto = session.ToWorkoutDto(HealthDataSdk.GoogleHealthConnect, DateTimeOffset.UtcNow);

        // Assert
        Assert.NotNull(dto.Metadata);
        Assert.True(dto.Metadata.ContainsKey(WorkoutMetadata.PauseIntervals));
    }

    [Fact]
    public void ToWorkoutDto_WithExistingWorkout_PreservesMetrics()
    {
        // Arrange
        var session = CreateSession();
        var existingWorkout = new WorkoutDto
        {
            Id = "old-id",
            DataSdk = HealthDataSdk.GoogleHealthConnect,
            DataOrigin = "com.other",
            Timestamp = DateTimeOffset.UtcNow,
            ActivityType = ActivityType.Running,
            StartTime = DateTimeOffset.UtcNow.AddHours(-2),
            EnergyBurned = 750,
            Distance = 10000,
            AverageHeartRate = 145,
            MaxHeartRate = 180,
            MinHeartRate = 90,
        };

        // Act
        var dto = session.ToWorkoutDto(existingWorkout);

        // Assert
        Assert.Equal(750, dto.EnergyBurned);
        Assert.Equal(10000, dto.Distance);
        Assert.Equal(145, dto.AverageHeartRate);
        Assert.Equal(180, dto.MaxHeartRate);
        Assert.Equal(90, dto.MinHeartRate);
    }

    [Fact]
    public void ToWorkoutSession_CompletedWorkout_SetsEndedState()
    {
        // Arrange
        var dto = new WorkoutDto
        {
            Id = "w-1",
            DataSdk = HealthDataSdk.GoogleHealthConnect,
            DataOrigin = "com.test",
            Timestamp = DateTimeOffset.UtcNow.AddHours(-1),
            ActivityType = ActivityType.Swimming,
            StartTime = DateTimeOffset.UtcNow.AddHours(-1),
            EndTime = DateTimeOffset.UtcNow,
        };

        // Act
        var session = dto.ToWorkoutSession();

        // Assert
        Assert.NotNull(session);
        Assert.Equal(WorkoutSessionState.Ended, session.State);
        Assert.Equal("w-1", session.Id);
        Assert.Equal(ActivityType.Swimming, session.ActivityType);
    }

    [Fact]
    public void ToWorkoutSession_ActiveWorkout_SetsRunningState()
    {
        // Arrange
        var dto = new WorkoutDto
        {
            Id = "w-2",
            DataSdk = HealthDataSdk.GoogleHealthConnect,
            DataOrigin = "com.test",
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-30),
            ActivityType = ActivityType.Running,
            StartTime = DateTimeOffset.UtcNow.AddMinutes(-30),
            EndTime = null,
        };

        // Act
        var session = dto.ToWorkoutSession();

        // Assert
        Assert.NotNull(session);
        Assert.Equal(WorkoutSessionState.Running, session.State);
    }

    [Fact]
    public void ToWorkoutSession_WithPauseMetadata_RestoresPauseIntervals()
    {
        // Arrange
        var intervals = new List<DateRange>
        {
            new(DateTimeOffset.FromUnixTimeMilliseconds(1000), DateTimeOffset.FromUnixTimeMilliseconds(2000)),
        };
        var dto = new WorkoutDto
        {
            Id = "w-3",
            DataSdk = HealthDataSdk.GoogleHealthConnect,
            DataOrigin = "com.test",
            Timestamp = DateTimeOffset.UtcNow,
            ActivityType = ActivityType.Running,
            StartTime = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                { WorkoutMetadata.PauseIntervals, intervals.ToJson() }
            }
        };

        // Act
        var session = dto.ToWorkoutSession();

        // Assert
        Assert.NotNull(session);
        Assert.Single(session.PauseIntervals);
        Assert.Equal(intervals[0].Start, session.PauseIntervals[0].Start);
        Assert.Equal(intervals[0].End, session.PauseIntervals[0].End);
    }
}
