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

    /// <summary>
    /// How long a launch may leave the requesting activity untouched before it counts as never shown. Android pauses the
    /// activity as soon as it starts a screen on top of it, long before that screen is drawn, so the pause does not
    /// wait for Health Connect to start.
    /// </summary>
    internal static readonly TimeSpan LaunchWindow = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The permission controller is a package of its own and its name carries this on the devices checked
    /// (com.google.android.permissioncontroller on a Pixel 6 and a Galaxy A53, Android 16) and in AOSP
    /// (com.android.permissioncontroller).
    /// </summary>
    private const string PermissionControllerPackageMarker = "permissioncontroller";

    private readonly TaskCompletionSource _returnedWithoutAnswerSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TimeSpan _answerGracePeriod;
    private volatile bool _hasRequestingActivityLeft;
    private volatile bool _isRequestingActivityDestroyed;
    private int _activityPauseCount;

    internal PermissionScreenTracker(TimeSpan answerGracePeriod)
    {
        _answerGracePeriod = answerGracePeriod;
    }

    /// <summary>
    /// Whether Health Connect's permission screen was shown: the requesting activity was stopped, or an activity of
    /// another app than the permission controller was seen on top of the app's task. Either is enough, because on
    /// some devices (a Pixel 6 on Android 16) the screen only pauses the app's activity and never stops it. When
    /// Android declines to ask again (a permission the user refused twice), it only passes through the permission
    /// controller's transparent activity, which finishes at once, stops nothing and has no other activity above it.
    /// </summary>
    internal bool IsPermissionScreenShown { get; private set; }

    /// <summary>
    /// Completes once the app is back on screen and the answer can no longer arrive, or when the launch never put a
    /// screen on top of the app (see <see cref="OnLaunched"/>). Never while the permission screen is up: the user may
    /// take as long as they like, and the next request must not start under it.
    /// </summary>
    internal Task ReturnedWithoutAnswerTask => _returnedWithoutAnswerSource.Task;

    /// <summary>
    /// Call right after the screen was launched. Android drops a launch from an app that is in the background: nothing
    /// is shown and nothing is answered, so without this the wait would never end and the permission gate would stay
    /// held for every later request. A launch that touched the requesting activity within <paramref name="launchWindow"/>
    /// is left to the user. HealthService launches nothing from an activity that is not started, so this covers a
    /// launch Android still drops, such as one made in the moments after the user left the app.
    /// </summary>
    internal void OnLaunched(TimeSpan launchWindow)
    {
        _ = CompleteIfNothingCoveredTheActivity(launchWindow);
    }

    /// <summary>
    /// Any screen Android puts on top pauses the requesting activity, the transparent one included. Only a resume
    /// after this is a return from the screen: a launch made right after an earlier screen closed still sees the
    /// resume that closing caused, while its own screen is about to come up.
    /// </summary>
    internal void OnRequestingActivityPaused()
    {
        _hasRequestingActivityLeft = true;
    }

    internal void OnRequestingActivityStopped()
    {
        _hasRequestingActivityLeft = true;
        IsPermissionScreenShown = true;
    }

    /// <summary>
    /// Fed with the package of the activity on top of the app's task while the request waits. The launch goes to the
    /// permission controller, whose transparent activity either passes straight through or puts Health Connect's own
    /// screen on top of itself, so a third package on top is that screen. A null package (not known) says nothing.
    /// </summary>
    internal void OnTopActivityChanged(string? topActivityPackageName, string ownPackageName)
    {
        if (topActivityPackageName is null ||
            topActivityPackageName == ownPackageName ||
            topActivityPackageName.Contains(PermissionControllerPackageMarker, StringComparison.Ordinal))
        {
            return;
        }

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

    /// <summary>
    /// Any activity of the app pausing. Under a permission screen Android recreates a destroyed activity and resumes
    /// the new one for a moment before pausing it again (seen on a Pixel 6 after a font scale change, 3 ms apart), so a
    /// resume is only a return to the app when no pause follows it.
    /// </summary>
    internal void OnActivityPaused()
    {
        Interlocked.Increment(ref _activityPauseCount);
    }

    /// <summary>
    /// The app is back on screen when its requesting activity resumes after the screen came up, or when the activity
    /// that replaced a destroyed one resumes. Either way the answer can still be on its way, so the wait ends only
    /// after <see cref="AnswerGracePeriod"/> and only if no activity of the app paused again in the meantime.
    /// </summary>
    internal void OnActivityResumed(bool isRequestingActivity)
    {
        if (_isRequestingActivityDestroyed || (isRequestingActivity && _hasRequestingActivityLeft))
        {
            _ = CompleteAfterGracePeriod();
        }
    }

    private async Task CompleteAfterGracePeriod()
    {
        var activityPauseCountAtResume = Volatile.Read(ref _activityPauseCount);
        await Task.Delay(_answerGracePeriod);
        if (activityPauseCountAtResume == Volatile.Read(ref _activityPauseCount))
        {
            _returnedWithoutAnswerSource.TrySetResult();
        }
    }

    private async Task CompleteIfNothingCoveredTheActivity(TimeSpan launchWindow)
    {
        await Task.Delay(launchWindow);
        if (!_hasRequestingActivityLeft && !_isRequestingActivityDestroyed)
        {
            _returnedWithoutAnswerSource.TrySetResult();
        }
    }
}
