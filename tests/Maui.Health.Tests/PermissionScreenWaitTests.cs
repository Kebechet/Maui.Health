using Maui.Health.Platforms.Android.Helpers;
using Xunit;

namespace Maui.Health.Tests;

public class PermissionScreenWaitTests
{
    private static readonly TimeSpan _shortGracePeriod = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan _shortWait = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan _testTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task WaitForAnswer_ScreenAnswers_ReturnsTheAnswer()
    {
        // Arrange
        var answerSource = new TaskCompletionSource<string?>();
        var screenTracker = new PermissionScreenTracker(_shortGracePeriod);

        // Act
        var waitTask = PermissionScreenWait.WaitForAnswer(answerSource.Task, screenTracker);
        screenTracker.OnRequestingActivityStopped();
        answerSource.SetResult("granted");
        screenTracker.OnActivityResumed(isRequestingActivity: true);
        var answer = await waitTask.WaitAsync(_testTimeout);

        // Assert
        Assert.Equal("granted", answer);
        Assert.True(screenTracker.IsPermissionScreenShown);
    }

    /// <summary>
    /// With "Don't keep activities" the app's activity is destroyed as soon as the permission screen covers it. The
    /// user has not answered yet, so the wait must go on until the app is back on screen.
    /// </summary>
    [Fact]
    public async Task WaitForAnswer_RequestingActivityDestroyedUnderTheScreen_WaitsUntilTheAppResumes()
    {
        // Arrange
        var answerSource = new TaskCompletionSource<string?>();
        var screenTracker = new PermissionScreenTracker(_shortGracePeriod);

        // Act
        var waitTask = PermissionScreenWait.WaitForAnswer(answerSource.Task, screenTracker);
        screenTracker.OnRequestingActivityStopped();
        screenTracker.OnRequestingActivityDestroyed();
        await Task.Delay(_shortWait);
        var isCompletedWhileTheScreenIsUp = waitTask.IsCompleted;
        screenTracker.OnActivityResumed(isRequestingActivity: false);
        var answer = await waitTask.WaitAsync(_testTimeout);

        // Assert
        Assert.False(isCompletedWhileTheScreenIsUp);
        Assert.Null(answer);
    }

    /// <summary>
    /// An answer that never arrives ends the wait only once the requesting activity is back on screen, never while
    /// the permission screen is still up.
    /// </summary>
    [Fact]
    public async Task WaitForAnswer_AnswerLost_EndsOnlyAfterTheRequestingActivityResumes()
    {
        // Arrange
        var answerSource = new TaskCompletionSource<string?>();
        var screenTracker = new PermissionScreenTracker(_shortGracePeriod);

        // Act
        var waitTask = PermissionScreenWait.WaitForAnswer(answerSource.Task, screenTracker);
        screenTracker.OnRequestingActivityStopped();
        await Task.Delay(_shortWait);
        var isCompletedWhileTheScreenIsUp = waitTask.IsCompleted;
        screenTracker.OnActivityResumed(isRequestingActivity: true);
        var answer = await waitTask.WaitAsync(_testTimeout);

        // Assert
        Assert.False(isCompletedWhileTheScreenIsUp);
        Assert.Null(answer);
    }

    [Fact]
    public async Task WaitForAnswer_AnswerArrivesWithinTheGracePeriod_ReturnsTheAnswer()
    {
        // Arrange
        var answerSource = new TaskCompletionSource<string?>();
        var screenTracker = new PermissionScreenTracker(TimeSpan.FromSeconds(1));

        // Act
        var waitTask = PermissionScreenWait.WaitForAnswer(answerSource.Task, screenTracker);
        screenTracker.OnActivityResumed(isRequestingActivity: true);
        answerSource.SetResult("granted");
        var answer = await waitTask.WaitAsync(_testTimeout);

        // Assert
        Assert.Equal("granted", answer);
    }

    [Fact]
    public void IsPermissionScreenShown_RequestingActivityNeverStopped_IsFalse()
    {
        // Arrange
        var screenTracker = new PermissionScreenTracker(_shortGracePeriod);

        // Act
        screenTracker.OnActivityResumed(isRequestingActivity: true);

        // Assert
        Assert.False(screenTracker.IsPermissionScreenShown);
    }
}
