using AndroidX.Core.Content;
using Microsoft.Extensions.Logging;
using static Maui.Health.Platforms.Android.AndroidConstant;
using ActivityManager = Android.App.ActivityManager;
using Importance = Android.App.Importance;
using Permission = Android.Content.PM.Permission;

namespace Maui.Health.Platforms.Android.Helpers;

/// <summary>
/// Health Connect serves reads only to an app in the foreground unless it holds
/// <see cref="BackgroundReadPermission"/>, and refuses any other read with a SecurityException. The
/// health services check before reading, so such a read is not made at all, and log a refusal below
/// error level: it means only that the app is in the background, which is not a fault.
/// https://developer.android.com/health-and-fitness/guides/health-connect/develop/read-data#background-reads
/// </summary>
internal static class BackgroundReadGuard
{
    /// <summary>
    /// True, and logged, when a read must not be made now. Health Connect counts the app as foreground
    /// down to process importance visible, and checks the permission as a runtime permission, which
    /// Android 13 and lower report denied because they lack the feature. Uses the application context,
    /// so it still answers once the activity is gone.
    /// </summary>
    internal static bool IsReadBlocked(ILogger logger, string operationName)
    {
        var runningAppProcessInfo = new ActivityManager.RunningAppProcessInfo();
        ActivityManager.GetMyMemoryState(runningAppProcessInfo);
        if (runningAppProcessInfo.Importance <= Importance.Visible)
        {
            return false;
        }

        if (ContextCompat.CheckSelfPermission(Platform.AppContext, BackgroundReadPermission) == Permission.Granted)
        {
            return false;
        }

        logger.LogInformation("Skipped {Operation}: the app is in the background without {Permission}", operationName, BackgroundReadPermission);
        return true;
    }

    /// <summary>
    /// Logs a refusal Health Connect made itself, usually because the app left the foreground after
    /// <see cref="IsReadBlocked"/> passed.
    /// </summary>
    internal static void LogRefusal(ILogger logger, Exception exception, string operationName)
    {
        logger.LogInformation(exception, "Health Connect refused {Operation}: the app is in the background without {Permission}", operationName, BackgroundReadPermission);
    }
}
