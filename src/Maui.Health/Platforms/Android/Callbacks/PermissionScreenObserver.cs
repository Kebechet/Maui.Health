using Android.App;
using Android.OS;
using Maui.Health.Platforms.Android.Helpers;

namespace Maui.Health.Platforms.Android.Callbacks;

/// <summary>
/// Feeds the app's activity lifecycle to a <see cref="PermissionScreenTracker"/> while a permission request runs.
/// Registered on the application, so it sees only this app's activities.
/// </summary>
internal sealed class PermissionScreenObserver : Java.Lang.Object, global::Android.App.Application.IActivityLifecycleCallbacks
{
    private readonly Activity _requestingActivity;
    private readonly PermissionScreenTracker _screenTracker;

    public PermissionScreenObserver(Activity requestingActivity, PermissionScreenTracker screenTracker)
    {
        _requestingActivity = requestingActivity;
        _screenTracker = screenTracker;
    }

    public void OnActivityStopped(Activity activity)
    {
        if (ReferenceEquals(activity, _requestingActivity))
        {
            _screenTracker.OnRequestingActivityStopped();
        }
    }

    public void OnActivityDestroyed(Activity activity)
    {
        if (ReferenceEquals(activity, _requestingActivity))
        {
            _screenTracker.OnRequestingActivityDestroyed();
        }
    }

    public void OnActivityResumed(Activity activity)
    {
        _screenTracker.OnActivityResumed(ReferenceEquals(activity, _requestingActivity));
    }

    public void OnActivityCreated(Activity activity, Bundle? savedInstanceState)
    {
    }

    public void OnActivityPaused(Activity activity)
    {
        if (ReferenceEquals(activity, _requestingActivity))
        {
            _screenTracker.OnRequestingActivityPaused();
        }
    }

    public void OnActivitySaveInstanceState(Activity activity, Bundle outState)
    {
    }

    public void OnActivityStarted(Activity activity)
    {
    }
}
