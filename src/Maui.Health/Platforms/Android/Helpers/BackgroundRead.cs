using AndroidX.Core.Content;
using AndroidX.Health.Connect.Client;
using Maui.Health.Enums.Errors;
using Maui.Health.Models;
using Maui.Health.Platforms.Android.Extensions;
using Microsoft.Extensions.Logging;
using static Maui.Health.Platforms.Android.AndroidConstant;
using ActivityManager = Android.App.ActivityManager;
using Importance = Android.App.Importance;
using Permission = Android.Content.PM.Permission;

namespace Maui.Health.Platforms.Android.Helpers;

/// <summary>
/// Health Connect serves reads only to an app in the foreground unless it holds
/// <see cref="BackgroundReadPermission"/>. Reads are always made and Health Connect's answer decides; these helpers
/// only interpret that answer, so they cannot stop a read Health Connect would have served. A refusal is logged below
/// error level: it means only that the app is in the background, which is not a fault.
/// https://developer.android.com/health-and-fitness/guides/health-connect/develop/read-data#background-reads
/// </summary>
internal static class BackgroundRead
{
    /// <summary>
    /// Android 14, the first version that ships Health Connect as part of the system. Not
    /// <c>BuildVersionCodes.UpsideDownCake</c>, which is itself marked as available only from Android 14 and so
    /// cannot guard a check for it (CA1416).
    /// </summary>
    private const int SystemHealthConnectApiLevel = 34;

    /// <summary>
    /// True when Health Connect refused or limited the read that threw <paramref name="exception"/> because the app
    /// is in the background without <see cref="BackgroundReadPermission"/>.
    /// </summary>
    internal static bool IsRefusal(Exception exception)
    {
        return exception.IsBackgroundReadRefusal(IsInBackgroundWithoutPermission);
    }

    /// <summary>
    /// True when a read that succeeded may hold only the app's own records. The Android 14 initial release does not
    /// refuse a background read but quietly limits it to the caller's records; versions that support
    /// <see cref="BackgroundReadPermission"/> refuse it instead, which <see cref="IsRefusal"/> recognises. Android 13
    /// and lower run Health Connect as a separate app whose background rules are not public, so they are not judged.
    /// https://android.googlesource.com/platform/packages/modules/HealthFitness/+/refs/tags/android-14.0.0_r1/service/java/com/android/server/healthconnect/HealthConnectServiceImpl.java
    /// </summary>
    internal static bool IsReadPossiblyIncomplete(IHealthConnectClient healthConnectClient)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(SystemHealthConnectApiLevel))
        {
            return false;
        }

        if (healthConnectClient.IsBackgroundReadSupported())
        {
            return false;
        }

        return IsInBackgroundWithoutPermission();
    }

    /// <summary>
    /// The result for a read Health Connect refused or limited because the app is in the background, carrying
    /// <paramref name="exception"/> as <see cref="Result.ErrorException"/>.
    /// </summary>
    internal static TResult ToRefusedResult<TResult>(ILogger logger, string operationName, Exception exception)
        where TResult : Result<ReadHealthDataError>, new()
    {
        logger.LogInformation(exception, "{Operation} returned {Error}: the app is in the background without {Permission}",
            operationName, ReadHealthDataError.BackgroundReadNotPermitted, BackgroundReadPermission);

        return new TResult
        {
            Error = ReadHealthDataError.BackgroundReadNotPermitted,
            ErrorException = exception,
        };
    }

    /// <summary>
    /// The result for a read that succeeded but may hold only the app's own records, see
    /// <see cref="IsReadPossiblyIncomplete"/>.
    /// </summary>
    internal static TResult ToRefusedResult<TResult>(ILogger logger, string operationName)
        where TResult : Result<ReadHealthDataError>, new()
    {
        var exception = new InvalidOperationException(
            $"Health Connect limited {operationName} to this app's own records because the app is in the background.");

        return ToRefusedResult<TResult>(logger, operationName, exception);
    }

    /// <summary>
    /// True when Health Connect counts the app as background and it lacks <see cref="BackgroundReadPermission"/>.
    /// Health Connect asks AppOps, which counts process states up to a bound foreground service as foreground, and
    /// Android reports exactly those states as importance <see cref="Importance.ForegroundService"/> or higher.
    /// AppOps also keeps the app in the foreground for a few seconds after it leaves, which importance does not
    /// show, so this answer is only fit for interpreting a call Health Connect has already answered.
    /// https://android.googlesource.com/platform/frameworks/base/+/refs/heads/main/services/core/java/com/android/server/appop/AppOpsUidStateTracker.java
    /// https://android.googlesource.com/platform/frameworks/base/+/refs/heads/main/core/java/android/app/ActivityManager.java
    /// </summary>
    private static bool IsInBackgroundWithoutPermission()
    {
        var runningAppProcessInfo = new ActivityManager.RunningAppProcessInfo();
        ActivityManager.GetMyMemoryState(runningAppProcessInfo);
        if (runningAppProcessInfo.Importance <= Importance.ForegroundService)
        {
            return false;
        }

        return ContextCompat.CheckSelfPermission(Platform.AppContext, BackgroundReadPermission) != Permission.Granted;
    }
}
