using Android.App;
using Android.OS;

namespace Maui.Health.Platforms.Android.Callbacks;

/// <summary>
/// Watches the activity that launched a permission request while the request runs.
/// </summary>
internal sealed class PermissionScreenObserver : Java.Lang.Object, global::Android.App.Application.IActivityLifecycleCallbacks
{
    private readonly Activity _requestingActivity;
    private readonly TaskCompletionSource _requestingActivityDestroyedSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public PermissionScreenObserver(Activity requestingActivity)
    {
        _requestingActivity = requestingActivity;
    }

    /// <summary>
    /// Whether the requesting activity was stopped. Health Connect's permission screen is a full-screen activity, so
    /// showing it stops the app's activity. When Android declines to ask again (a permission the user refused twice),
    /// it only passes through a transparent activity that finishes at once and stops nothing.
    /// </summary>
    public bool IsPermissionScreenShown { get; private set; }

    /// <summary>
    /// Completes when the requesting activity is destroyed. Its request then gets no answer: Android delivers it to
    /// the activity recreated in its place, which knows nothing of the request.
    /// </summary>
    public Task RequestingActivityDestroyedTask => _requestingActivityDestroyedSource.Task;

    public void OnActivityStopped(Activity activity)
    {
        if (ReferenceEquals(activity, _requestingActivity))
        {
            IsPermissionScreenShown = true;
        }
    }

    public void OnActivityDestroyed(Activity activity)
    {
        if (ReferenceEquals(activity, _requestingActivity))
        {
            _requestingActivityDestroyedSource.TrySetResult();
        }
    }

    public void OnActivityCreated(Activity activity, Bundle? savedInstanceState)
    {
    }

    public void OnActivityPaused(Activity activity)
    {
    }

    public void OnActivityResumed(Activity activity)
    {
    }

    public void OnActivitySaveInstanceState(Activity activity, Bundle outState)
    {
    }

    public void OnActivityStarted(Activity activity)
    {
    }
}
