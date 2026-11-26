namespace Maui.Health.Platforms.Android.Helpers;

/// <summary>
/// Waits for the answer of Health Connect's permission screen. Touches no Android API, so the tests link it as source.
/// </summary>
internal static class PermissionScreenWait
{
    /// <summary>
    /// Waits until the screen answers or <paramref name="screenTracker"/> sees the app back on screen without an
    /// answer, or sees that the launch never covered the app. There is no timeout while the screen is up: the
    /// permission gate stays held until the user is done, so a queued request cannot launch a second screen from the
    /// background.
    /// </summary>
    /// <returns>The answer, or <c>default</c> when it did not arrive; the caller then reads the grants from Health Connect.</returns>
    internal static async Task<TAnswer?> WaitForAnswer<TAnswer>(Task<TAnswer?> answerTask, PermissionScreenTracker screenTracker)
    {
        var completedTask = await Task.WhenAny(answerTask, screenTracker.ReturnedWithoutAnswerTask);

        return completedTask == answerTask
            ? await answerTask
            : default;
    }
}
