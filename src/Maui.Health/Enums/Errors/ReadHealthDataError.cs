namespace Maui.Health.Enums.Errors;

/// <summary>
/// Read failures a caller is expected to handle differently from a generic failure. Any other
/// failed read leaves <see cref="Models.Result{TError}.Error"/> null and carries the platform
/// exception on <see cref="Models.Result.ErrorException"/>.
/// </summary>
public enum ReadHealthDataError
{
    /// <summary>
    /// Health Connect serves reads only to an app in the foreground unless it holds
    /// READ_HEALTH_DATA_IN_BACKGROUND. The app was in the background without it, so the read was not
    /// made, or Health Connect refused it. The same read succeeds once the app is in the foreground
    /// again; to read from the background, request the permission with
    /// canRequestBackgroundReadPermission. Android only.
    /// https://developer.android.com/health-and-fitness/guides/health-connect/develop/read-data#background-reads
    /// </summary>
    BackgroundReadNotPermitted,
}
