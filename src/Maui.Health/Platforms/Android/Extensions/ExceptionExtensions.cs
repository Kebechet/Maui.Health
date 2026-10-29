using static Maui.Health.Platforms.Android.AndroidConstant;

namespace Maui.Health.Platforms.Android.Extensions;

internal static class ExceptionExtensions
{
    /// <summary>
    /// True when Health Connect refused a read, or limited it to the app's own records, because the app is not in
    /// the foreground and lacks <see cref="BackgroundReadPermission"/>.
    /// <para>A refusal that names the permission, or says the app <see cref="HealthConnectMessage.ForegroundRequired"/>
    /// (Android 14 initial release), is one regardless of app state. <see cref="HealthConnectMessage.OtherAppsReadRefused"/>
    /// and <see cref="HealthConnectMessage.NoRecords"/> also follow from write-only access or a missing record, so
    /// they count only when <paramref name="isInBackgroundWithoutPermission"/> confirms the app is in the background
    /// without the permission. That check runs after Health Connect has answered, so it can only relabel a failed
    /// read, never prevent one.</para>
    /// <para>Only the message reaches managed code: the coroutine continuation rethrows the Throwable as a plain
    /// Exception, and a call that fails before suspending arrives as the cause of the reflective invocation's
    /// exception, so the inner exceptions are searched too.</para>
    /// https://developer.android.com/health-and-fitness/guides/health-connect/develop/read-data#background-reads
    /// </summary>
    internal static bool IsBackgroundReadRefusal(this Exception exception, Func<bool> isInBackgroundWithoutPermission)
    {
        if (exception.HasMessageContaining(BackgroundReadPermission) ||
            exception.HasMessageContaining(HealthConnectMessage.ForegroundRequired))
        {
            return true;
        }

        var isSelfReadRestriction = exception.HasMessageContaining(HealthConnectMessage.OtherAppsReadRefused) ||
            exception.HasMessageContaining(HealthConnectMessage.NoRecords);

        return isSelfReadRestriction && isInBackgroundWithoutPermission();
    }

    private static bool HasMessageContaining(this Exception exception, string text)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current.Message.Contains(text, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
