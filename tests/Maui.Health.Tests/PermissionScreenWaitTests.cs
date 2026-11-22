using Maui.Health.Platforms.Android.Helpers;
using Xunit;

namespace Maui.Health.Tests;

public class PermissionScreenWaitTests
{
    private static readonly TimeSpan _longTimeout = TimeSpan.FromMinutes(1);

    [Fact]
    public async Task WaitForAnswer_ScreenAnswers_ReturnsTheAnswer()
    {
        // Arrange
        var answerSource = new TaskCompletionSource<string?>();
        var activityDestroyedSource = new TaskCompletionSource();

        // Act
        var waitTask = PermissionScreenWait.WaitForAnswer(answerSource.Task, activityDestroyedSource.Task, _longTimeout);
        answerSource.SetResult("granted");
        var answer = await waitTask;

        // Assert
        Assert.Equal("granted", answer);
    }

    /// <summary>
    /// A requesting activity destroyed under the permission screen never gets the answer; without this the request
    /// and the permission gate would wait until the app restarts.
    /// </summary>
    [Fact]
    public async Task WaitForAnswer_RequestingActivityDestroyed_ReturnsWithoutAnAnswer()
    {
        // Arrange
        var answerSource = new TaskCompletionSource<string?>();
        var activityDestroyedSource = new TaskCompletionSource();

        // Act
        var waitTask = PermissionScreenWait.WaitForAnswer(answerSource.Task, activityDestroyedSource.Task, _longTimeout);
        activityDestroyedSource.SetResult();
        var answer = await waitTask.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        Assert.Null(answer);
    }

    [Fact]
    public async Task WaitForAnswer_NoAnswerWithinTimeout_ReturnsWithoutAnAnswer()
    {
        // Arrange
        var answerSource = new TaskCompletionSource<string?>();
        var activityDestroyedSource = new TaskCompletionSource();

        // Act
        var answer = await PermissionScreenWait
            .WaitForAnswer(answerSource.Task, activityDestroyedSource.Task, TimeSpan.FromMilliseconds(50))
            .WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        Assert.Null(answer);
    }
}
