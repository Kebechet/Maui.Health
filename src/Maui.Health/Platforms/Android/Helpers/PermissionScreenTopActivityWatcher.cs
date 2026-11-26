using Android.App;
using Android.Content;

namespace Maui.Health.Platforms.Android.Helpers;

/// <summary>
/// Reports the activity on top of the app's task to a <see cref="PermissionScreenTracker"/> while a permission request
/// waits, so a screen that only pauses the app's activity is still seen. Best effort: when Android does not tell
/// (an older version, a refused call) the tracker keeps to what the lifecycle says.
/// </summary>
internal static class PermissionScreenTopActivityWatcher
{
    /// <summary>
    /// Health Connect's screen stays on top until the user answers, so it is not missed between two samples. The
    /// pass-through of a declined request is on top for about a tenth of a second and needs no sampling.
    /// </summary>
    internal static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(200);

    internal static async Task Watch(PermissionScreenTracker screenTracker, int taskId, CancellationToken stopToken)
    {
        // https://developer.android.com/reference/android/app/ActivityManager.RecentTaskInfo#topActivity
        if (!OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            return;
        }

        var context = Platform.AppContext;
        var activityManager = context.GetSystemService(Context.ActivityService) as ActivityManager;
        var ownPackageName = context.PackageName;
        if (activityManager is null || ownPackageName is null)
        {
            return;
        }

        while (!stopToken.IsCancellationRequested)
        {
            try
            {
                var taskInfo = activityManager.AppTasks?
                    .Select(appTask => appTask.TaskInfo)
                    .FirstOrDefault(info => info is not null && GetTaskId(info) == taskId);
                screenTracker.OnTopActivityChanged(taskInfo?.TopActivity?.PackageName, ownPackageName);
            }
            catch (Exception)
            {
                // Android refused the call: the lifecycle signal of the tracker still works, this one is only an addition.
                return;
            }

            try
            {
                await Task.Delay(PollInterval, stopToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>
    /// The task id is <c>taskId</c> from Android 10 on; the older <c>id</c> is deprecated there.
    /// </summary>
    private static int GetTaskId(ActivityManager.RecentTaskInfo taskInfo)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            return taskInfo.TaskId;
        }

        return taskInfo.Id;
    }
}
