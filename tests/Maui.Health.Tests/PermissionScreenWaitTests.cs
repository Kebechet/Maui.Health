using Maui.Health.Platforms.Android.Helpers;
using Xunit;

namespace Maui.Health.Tests;

public class PermissionScreenWaitTests
{
    private static readonly TimeSpan _shortGracePeriod = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan _shortLaunchWindow = TimeSpan.FromMilliseconds(50);
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
        screenTracker.OnRequestingActivityPaused();
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
        screenTracker.OnRequestingActivityPaused();
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
        screenTracker.OnRequestingActivityPaused();
        screenTracker.OnRequestingActivityStopped();
        await Task.Delay(_shortWait);
        var isCompletedWhileTheScreenIsUp = waitTask.IsCompleted;
        screenTracker.OnActivityResumed(isRequestingActivity: true);
        var answer = await waitTask.WaitAsync(_testTimeout);

        // Assert
        Assert.False(isCompletedWhileTheScreenIsUp);
        Assert.Null(answer);
    }

    /// <summary>
    /// A second launch made right after the first screen closed sees the resume that closing caused, before its own
    /// screen pauses the activity. That resume is no return from the new screen, which is still up: on a Samsung A53
    /// the wait ended under the extras screen and reported the extras the user then granted as denied.
    /// </summary>
    [Fact]
    public async Task WaitForAnswer_ResumeBeforeTheScreenCoveredTheActivity_KeepsWaiting()
    {
        // Arrange
        var answerSource = new TaskCompletionSource<string?>();
        var screenTracker = new PermissionScreenTracker(_shortGracePeriod);

        // Act
        var waitTask = PermissionScreenWait.WaitForAnswer(answerSource.Task, screenTracker);
        screenTracker.OnActivityResumed(isRequestingActivity: true);
        screenTracker.OnRequestingActivityPaused();
        screenTracker.OnRequestingActivityStopped();
        await Task.Delay(_shortWait);
        var isCompletedWhileTheScreenIsUp = waitTask.IsCompleted;
        answerSource.SetResult("granted");
        var answer = await waitTask.WaitAsync(_testTimeout);

        // Assert
        Assert.False(isCompletedWhileTheScreenIsUp);
        Assert.Equal("granted", answer);
    }

    [Fact]
    public async Task WaitForAnswer_AnswerArrivesWithinTheGracePeriod_ReturnsTheAnswer()
    {
        // Arrange
        var answerSource = new TaskCompletionSource<string?>();
        var screenTracker = new PermissionScreenTracker(TimeSpan.FromSeconds(1));

        // Act
        var waitTask = PermissionScreenWait.WaitForAnswer(answerSource.Task, screenTracker);
        screenTracker.OnRequestingActivityPaused();
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

    /// <summary>
    /// A launch Android drops (the app is in the background) pauses nothing and answers nothing. The wait must not
    /// hold the permission gate for good, or every later request and every read that checks permissions queues behind it.
    /// </summary>
    [Fact]
    public async Task WaitForAnswer_LaunchPausedNothing_EndsAfterTheLaunchWindow()
    {
        // Arrange
        var answerSource = new TaskCompletionSource<string?>();
        var screenTracker = new PermissionScreenTracker(_shortGracePeriod);

        // Act
        var waitTask = PermissionScreenWait.WaitForAnswer(answerSource.Task, screenTracker);
        screenTracker.OnLaunched(_shortLaunchWindow);
        var answer = await waitTask.WaitAsync(_testTimeout);

        // Assert
        Assert.Null(answer);
        Assert.False(screenTracker.IsPermissionScreenShown);
    }

    [Fact]
    public async Task WaitForAnswer_ScreenPausedTheActivityWithinTheLaunchWindow_KeepsWaitingForTheUser()
    {
        // Arrange
        var answerSource = new TaskCompletionSource<string?>();
        var screenTracker = new PermissionScreenTracker(_shortGracePeriod);

        // Act
        var waitTask = PermissionScreenWait.WaitForAnswer(answerSource.Task, screenTracker);
        // The state is set before the window starts, so a stalled test thread cannot let the 50 ms timer run first.
        screenTracker.OnRequestingActivityPaused();
        screenTracker.OnRequestingActivityStopped();
        screenTracker.OnLaunched(_shortLaunchWindow);
        await Task.Delay(_shortWait);
        var isCompletedWhileTheScreenIsUp = waitTask.IsCompleted;
        answerSource.SetResult("granted");
        var answer = await waitTask.WaitAsync(_testTimeout);

        // Assert
        Assert.False(isCompletedWhileTheScreenIsUp);
        Assert.Equal("granted", answer);
    }

    [Fact]
    public async Task WaitForAnswer_RequestingActivityDestroyedWithinTheLaunchWindow_KeepsWaitingForTheApp()
    {
        // Arrange
        var answerSource = new TaskCompletionSource<string?>();
        var screenTracker = new PermissionScreenTracker(_shortGracePeriod);

        // Act
        var waitTask = PermissionScreenWait.WaitForAnswer(answerSource.Task, screenTracker);
        // The state is set before the window starts, so a stalled test thread cannot let the 50 ms timer run first.
        screenTracker.OnRequestingActivityDestroyed();
        screenTracker.OnLaunched(_shortLaunchWindow);
        await Task.Delay(_shortWait);
        var isCompletedWhileTheScreenIsUp = waitTask.IsCompleted;
        screenTracker.OnActivityResumed(isRequestingActivity: false);
        var answer = await waitTask.WaitAsync(_testTimeout);

        // Assert
        Assert.False(isCompletedWhileTheScreenIsUp);
        Assert.Null(answer);
    }

    /// <summary>
    /// On a Pixel 6 (Android 16) Health Connect's screen only pauses the app's activity and never stops it, so the
    /// activity on top of the app's task is what shows the screen.
    /// </summary>
    [Fact]
    public void IsPermissionScreenShown_HealthConnectActivityOnTopWithoutTheActivityStopping_IsTrue()
    {
        // Arrange
        var screenTracker = new PermissionScreenTracker(_shortGracePeriod);
        screenTracker.OnRequestingActivityPaused();

        // Act
        screenTracker.OnTopActivityChanged("com.google.android.permissioncontroller", "com.example.app");
        screenTracker.OnTopActivityChanged("com.google.android.healthconnect.controller", "com.example.app");

        // Assert
        Assert.True(screenTracker.IsPermissionScreenShown);
    }

    /// <summary>
    /// A request Android declines to ask again passes through the permission controller's transparent activity and
    /// never shows Health Connect's screen.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("com.example.app")]
    [InlineData("com.google.android.permissioncontroller")]
    [InlineData("com.android.permissioncontroller")]
    public void IsPermissionScreenShown_OnlyTheAppOrThePermissionControllerOnTop_IsFalse(string? topActivityPackageName)
    {
        // Arrange
        var screenTracker = new PermissionScreenTracker(_shortGracePeriod);
        screenTracker.OnRequestingActivityPaused();

        // Act
        screenTracker.OnTopActivityChanged(topActivityPackageName, "com.example.app");

        // Assert
        Assert.False(screenTracker.IsPermissionScreenShown);
    }

    /// <summary>
    /// Android recreates a destroyed activity under the permission screen and resumes the new one for a moment before
    /// pausing it again (a Pixel 6 after a font scale change). That is not the user coming back: the wait goes on until
    /// the app resumes and stays.
    /// </summary>
    [Fact]
    public async Task WaitForAnswer_ReplacementActivityResumedAndPausedAtOnce_KeepsWaitingForTheUser()
    {
        // Arrange
        var answerSource = new TaskCompletionSource<string?>();
        var screenTracker = new PermissionScreenTracker(TimeSpan.FromMilliseconds(300));
        screenTracker.OnRequestingActivityPaused();

        // Act
        var waitTask = PermissionScreenWait.WaitForAnswer(answerSource.Task, screenTracker);
        screenTracker.OnRequestingActivityDestroyed();
        screenTracker.OnActivityResumed(isRequestingActivity: false);
        screenTracker.OnActivityPaused();
        await Task.Delay(TimeSpan.FromMilliseconds(600));
        var isCompletedWhileTheScreenIsUp = waitTask.IsCompleted;
        screenTracker.OnActivityResumed(isRequestingActivity: false);
        var answer = await waitTask.WaitAsync(_testTimeout);

        // Assert
        Assert.False(isCompletedWhileTheScreenIsUp);
        Assert.Null(answer);
    }
}
