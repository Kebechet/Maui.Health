using Maui.Health.Services;
using Xunit;

namespace Maui.Health.Tests;

public class PermissionRequestGateTests
{
    private const string CancelledResult = "cancelled";

    private static readonly TimeSpan _shortWait = TimeSpan.FromMilliseconds(100);

    [Fact]
    public async Task Run_SecondRequestWhileFirstRuns_StartsOnlyAfterFirstFinishes()
    {
        // Arrange
        var gate = new PermissionRequestGate();
        var firstAnswerSource = new TaskCompletionSource<string>();
        var isSecondRequestStarted = false;

        // Act
        var firstRun = gate.Run(() => firstAnswerSource.Task, () => CancelledResult, CancellationToken.None);
        var secondRun = gate.Run(
            () =>
            {
                isSecondRequestStarted = true;
                return Task.FromResult("second");
            },
            () => CancelledResult,
            CancellationToken.None);
        await Task.Delay(_shortWait);
        var isSecondRequestStartedBeforeFirstAnswer = isSecondRequestStarted;
        firstAnswerSource.SetResult("first");

        // Assert
        Assert.False(isSecondRequestStartedBeforeFirstAnswer);
        Assert.Equal("first", await firstRun);
        Assert.Equal("second", await secondRun);
    }

    [Fact]
    public async Task Run_TokenCancelledWhileQueued_ReturnsCancelledWithoutRunningTheRequest()
    {
        // Arrange
        var gate = new PermissionRequestGate();
        var firstAnswerSource = new TaskCompletionSource<string>();
        using var cancellationSource = new CancellationTokenSource();
        var isQueuedRequestStarted = false;
        _ = gate.Run(() => firstAnswerSource.Task, () => CancelledResult, CancellationToken.None);

        // Act
        var queuedRun = gate.Run(
            () =>
            {
                isQueuedRequestStarted = true;
                return Task.FromResult("queued");
            },
            () => CancelledResult,
            cancellationSource.Token);
        cancellationSource.Cancel();
        var queuedResult = await queuedRun;
        firstAnswerSource.SetResult("first");
        await Task.Delay(_shortWait);

        // Assert
        Assert.Equal(CancelledResult, queuedResult);
        Assert.False(isQueuedRequestStarted);
    }

    /// <summary>
    /// A permission screen cannot be withdrawn. The caller stops waiting, but the gate stays held until the screen
    /// answers, so a later request cannot stack a second screen on top of it.
    /// </summary>
    [Fact]
    public async Task Run_TokenCancelledWhileRequestRuns_ReturnsCancelledButHoldsTheGateUntilTheRequestFinishes()
    {
        // Arrange
        var gate = new PermissionRequestGate();
        var screenAnswerSource = new TaskCompletionSource<string>();
        using var cancellationSource = new CancellationTokenSource();
        var isNextRequestStarted = false;

        // Act
        var cancelledRun = gate.Run(() => screenAnswerSource.Task, () => CancelledResult, cancellationSource.Token);
        cancellationSource.Cancel();
        var cancelledResult = await cancelledRun;
        var nextRun = gate.Run(
            () =>
            {
                isNextRequestStarted = true;
                return Task.FromResult("next");
            },
            () => CancelledResult,
            CancellationToken.None);
        await Task.Delay(_shortWait);
        var isNextRequestStartedBeforeScreenAnswer = isNextRequestStarted;
        screenAnswerSource.SetResult("answered");

        // Assert
        Assert.Equal(CancelledResult, cancelledResult);
        Assert.False(isNextRequestStartedBeforeScreenAnswer);
        Assert.Equal("next", await nextRun);
    }

    [Fact]
    public async Task Run_RequestThrows_ReleasesTheGate()
    {
        // Arrange
        var gate = new PermissionRequestGate();

        // Act
        var failedRun = gate.Run<string>(() => throw new InvalidOperationException(), () => CancelledResult, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => failedRun);
        var nextResult = await gate
            .Run(() => Task.FromResult("next"), () => CancelledResult, CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        Assert.Equal("next", nextResult);
    }
}
