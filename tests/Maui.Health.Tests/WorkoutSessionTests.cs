using Maui.Health.Enums;
using Maui.Health.Models;
using Xunit;

namespace Maui.Health.Tests;

public class WorkoutSessionTests
{
    private static WorkoutSession CreateSession(WorkoutSessionState state = WorkoutSessionState.Running)
    {
        return new WorkoutSession(
            "test-id",
            ActivityType.Running,
            "Morning Run",
            "com.test",
            DateTimeOffset.UtcNow.AddMinutes(-30),
            state
        );
    }

    [Fact]
    public void Pause_RunningSession_TransitionsToPaused()
    {
        // Arrange
        var session = CreateSession();

        // Act
        session.Pause();

        // Assert
        Assert.Equal(WorkoutSessionState.Paused, session.State);
        Assert.Single(session.PauseIntervals);
        Assert.False(session.PauseIntervals[0].IsClosed);
    }

    [Fact]
    public void Pause_PausedSession_ThrowsInvalidOperation()
    {
        // Arrange
        var session = CreateSession();
        session.Pause();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => session.Pause());
    }

    [Fact]
    public void Pause_EndedSession_ThrowsInvalidOperation()
    {
        // Arrange
        var session = CreateSession();
        session.End();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => session.Pause());
    }

    [Fact]
    public void Resume_PausedSession_TransitionsToRunning()
    {
        // Arrange
        var session = CreateSession();
        session.Pause();

        // Act
        session.Resume();

        // Assert
        Assert.Equal(WorkoutSessionState.Running, session.State);
        Assert.Single(session.PauseIntervals);
        Assert.True(session.PauseIntervals[0].IsClosed);
    }

    [Fact]
    public void Resume_RunningSession_ThrowsInvalidOperation()
    {
        // Arrange
        var session = CreateSession();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => session.Resume());
    }

    [Fact]
    public void End_RunningSession_TransitionsToEnded()
    {
        // Arrange
        var session = CreateSession();

        // Act
        session.End();

        // Assert
        Assert.Equal(WorkoutSessionState.Ended, session.State);
    }

    [Fact]
    public void End_PausedSession_ClosesOpenPauseInterval()
    {
        // Arrange
        var session = CreateSession();
        session.Pause();

        // Act
        session.End();

        // Assert
        Assert.Equal(WorkoutSessionState.Ended, session.State);
        Assert.Single(session.PauseIntervals);
        Assert.True(session.PauseIntervals[0].IsClosed);
    }

    [Fact]
    public void TotalPausedSeconds_MultiplePauses_SumsCorrectly()
    {
        // Arrange
        var pauseIntervals = new List<DateRange>
        {
            new(DateTimeOffset.UtcNow.AddMinutes(-20), DateTimeOffset.UtcNow.AddMinutes(-18)),
            new(DateTimeOffset.UtcNow.AddMinutes(-10), DateTimeOffset.UtcNow.AddMinutes(-8)),
        };
        var session = new WorkoutSession(
            "test-id", ActivityType.Running, null, "com.test",
            DateTimeOffset.UtcNow.AddMinutes(-30),
            WorkoutSessionState.Running,
            pauseIntervals
        );

        // Act
        var totalPaused = session.TotalPausedSeconds;

        // Assert
        Assert.Equal(240, totalPaused, precision: 1);
    }

    [Fact]
    public void TotalPausedSeconds_NoPauses_ReturnsZero()
    {
        // Arrange
        var session = CreateSession();

        // Act
        var totalPaused = session.TotalPausedSeconds;

        // Assert
        Assert.Equal(0, totalPaused);
    }

    [Fact]
    public void PauseResume_MultipleCycles_TracksAllIntervals()
    {
        // Arrange
        var session = CreateSession();

        // Act
        session.Pause();
        session.Resume();
        session.Pause();
        session.Resume();

        // Assert
        Assert.Equal(WorkoutSessionState.Running, session.State);
        Assert.Equal(2, session.PauseIntervals.Count);
        Assert.True(session.PauseIntervals[0].IsClosed);
        Assert.True(session.PauseIntervals[1].IsClosed);
    }
}
