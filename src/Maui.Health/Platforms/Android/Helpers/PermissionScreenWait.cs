namespace Maui.Health.Platforms.Android.Helpers;

/// <summary>
/// Waits for the answer of Health Connect's permission screen. Touches no Android API, so the tests link it as source.
/// </summary>
internal static class PermissionScreenWait
{
    /// <summary>
    /// Last resort for an answer that never arrives, so the permission gate cannot stay held until the app restarts.
    /// Long enough that nobody still reading the permission screen is cut off.
    /// </summary>
    internal static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Waits until the screen answers, the activity that launched it is destroyed, or <paramref name="timeout"/>
    /// passes. A destroyed activity never gets the answer: Android delivers it to the activity recreated in its place
    /// (a locale or font change, "Don't keep activities", memory pressure), whose registry knows nothing of this
    /// request. The caller then reads what was granted from Health Connect instead of from the answer.
    /// </summary>
    /// <returns>The answer, or <c>default</c> when it did not arrive.</returns>
    internal static async Task<TAnswer?> WaitForAnswer<TAnswer>(Task<TAnswer?> answerTask, Task requestingActivityDestroyedTask, TimeSpan timeout)
    {
        using var timeoutCancellation = new CancellationTokenSource();
        var timeoutTask = Task.Delay(timeout, timeoutCancellation.Token);
        var completedTask = await Task.WhenAny(answerTask, requestingActivityDestroyedTask, timeoutTask);
        timeoutCancellation.Cancel();

        return completedTask == answerTask
            ? await answerTask
            : default;
    }
}
