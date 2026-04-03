namespace Maui.Health.Enums;

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
    /// The permission has been denied by the user.
    /// </summary>
    Denied,
    /// <summary>
    /// The permission status cannot be determined.
    /// On iOS, read permissions always return this value because Apple does not expose read authorization status.
    /// On unsupported platforms (Windows, MacCatalyst), all permissions return this value.
    /// </summary>
    NotDetermined
}
