using static Maui.Health.Platforms.Android.AndroidConstant;

namespace Maui.Health.Platforms.Android.Extensions;

internal static class ExceptionExtensions
{
    /// <summary>
    /// True when Health Connect refused a read because the app is not in the foreground and lacks
    /// <see cref="BackgroundReadPermission"/>. The platform raises a SecurityException whose message ends
    /// in "does not have android.permission.health.READ_HEALTH_DATA_IN_BACKGROUND.", and that message is
    /// all that reaches managed code: the coroutine continuation rethrows the Throwable as a plain
    /// Exception, and a call that fails before suspending arrives as the cause of the reflective
    /// invocation's exception, so the inner exceptions are searched too.
    /// https://developer.android.com/health-and-fitness/guides/health-connect/develop/read-data#background-reads
    /// </summary>
    internal static bool IsBackgroundReadRefusal(this Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current.Message.Contains(BackgroundReadPermission, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
