namespace Kebechet.Types.Health.Enums.Errors;

/// <summary>
/// Errors that can occur when opening the platform screen where the user manages the app's health permissions.
/// </summary>
public enum OpenPermissionSettingsError
{
    /// <summary>
    /// The platform has no screen an app can open for its health permissions. On iOS the user manages them in the
    /// Health app or in Settings > Health > Data Access &amp; Devices, which no URL opens.
    /// </summary>
    NotSupported = 1,

    /// <summary>
    /// Android only: Health Connect is unavailable or needs an update.
    /// </summary>
    SdkUnavailable = 2,

    /// <summary>
    /// Opening the screen threw; the exception is on <see cref="Models.Result.ErrorException"/>.
    /// </summary>
    UnexpectedException = 3,
}
