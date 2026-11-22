namespace Kebechet.Types.Health.Enums;

/// <summary>
/// Authorization status of a health permission.
/// </summary>
public enum HealthPermissionStatus
{
    /// <summary>
    /// The permission has been granted by the user.
    /// </summary>
    Granted,
    /// <summary>
    /// The permission is not granted. On Android this also covers a permission the user was never asked for:
    /// Health Connect only reports which permissions are granted.
    /// </summary>
    Denied,
    /// <summary>
    /// iOS only: the user has not been asked for this write permission yet.
    /// </summary>
    NotDetermined,
    /// <summary>
    /// iOS only: HealthKit never discloses whether a read permission was granted. A read the user refused returns
    /// no data instead of failing, so reading is safe.
    /// https://developer.apple.com/documentation/healthkit/hkhealthstore/authorizationstatus(for:)
    /// </summary>
    Undisclosed,
    /// <summary>
    /// The status could not be read: the health platform is unavailable or unsupported on this device, or querying
    /// it failed. Says nothing about what the user chose.
    /// </summary>
    Unavailable,
}
