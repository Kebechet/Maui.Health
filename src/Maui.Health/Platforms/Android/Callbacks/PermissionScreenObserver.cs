using Android.App;
using Android.OS;

namespace Maui.Health.Platforms.Android.Callbacks;

/// <summary>
/// Notices whether the activity that launched a permission request was stopped while the request ran. Health
/// Connect's permission screen is a full-screen activity, so showing it stops the app's activity. When Android
/// declines to ask again (a permission the user refused twice), it only passes through a transparent activity that
/// finishes at once and stops nothing.
/// </summary>
internal sealed class PermissionScreenObserver : Java.Lang.Object, global::Android.App.Application.IActivityLifecycleCallbacks
{
    private readonly Activity _requestingActivity;

    public PermissionScreenObserver(Activity requestingActivity)
    {
        _requestingActivity = requestingActivity;
    }

    public bool IsPermissionScreenShown { get; private set; }

    public void OnActivityStopped(Activity activity)
    {
        if (ReferenceEquals(activity, _requestingActivity))
        {
            IsPermissionScreenShown = true;
        }
    }

    public void OnActivityCreated(Activity activity, Bundle? savedInstanceState)
    {
    }

    public void OnActivityDestroyed(Activity activity)
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
