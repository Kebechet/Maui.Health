namespace Maui.Health.Platforms.Android.Helpers;

/// <summary>
/// Follows the app's activities while Health Connect's permission screen is up, to tell when the screen is gone
/// without an answer. Fed by the Android lifecycle observer; touches no Android API itself, so the tests link it as
/// source.
/// </summary>
internal sealed class PermissionScreenTracker
{
    /// <summary>
    /// How long the requesting activity, back on screen, waits for an answer that has not come. Android hands the
    /// answer over before the activity resumes, so this only ends a wait whose answer was lost.
    /// </summary>
    internal static readonly TimeSpan AnswerGracePeriod = TimeSpan.FromSeconds(2);

    private readonly TaskCompletionSource _returnedWithoutAnswerSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TimeSpan _answerGracePeriod;
    private bool _isRequestingActivityDestroyed;

    internal PermissionScreenTracker(TimeSpan answerGracePeriod)
    {
        _answerGracePeriod = answerGracePeriod;
    }

    /// <summary>
    /// Whether the requesting activity was stopped. Health Connect's permission screen is a full-screen activity, so
    /// showing it stops the app's activity. When Android declines to ask again (a permission the user refused twice),
    /// it only passes through a transparent activity that finishes at once and stops nothing.
    /// </summary>
    internal bool IsPermissionScreenShown { get; private set; }

    /// <summary>
    /// Completes once the app is back on screen and the answer can no longer arrive. Never while the permission screen
    /// is up: the user may take as long as they like, and the next request must not start under it.
    /// </summary>
    internal Task ReturnedWithoutAnswerTask => _returnedWithoutAnswerSource.Task;

    internal void OnRequestingActivityStopped()
    {
        IsPermissionScreenShown = true;
    }

    /// <summary>
    /// Android delivers the answer to the activity recreated in place of a destroyed one ("Don't keep activities", a
    /// locale or font change, memory pressure), which knows nothing of this request, so the answer is lost.
    /// </summary>
    internal void OnRequestingActivityDestroyed()
    {
        _isRequestingActivityDestroyed = true;
    }

    internal void OnActivityResumed(bool isRequestingActivity)
    {
        if (_isRequestingActivityDestroyed)
        {
            _returnedWithoutAnswerSource.TrySetResult();
            return;
        }

        if (isRequestingActivity)
        {
            _ = CompleteAfterGracePeriod();
        }
    }

    private async Task CompleteAfterGracePeriod()
    {
        await Task.Delay(_answerGracePeriod);
        _returnedWithoutAnswerSource.TrySetResult();
    }
}
