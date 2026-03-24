namespace Maui.Health.Enums.Errors;

/// <summary>
/// Errors that can occur when requesting health data permissions from the user.
/// </summary>
public enum RequestPermissionError
{
    /// <summary>
    /// The health data platform is not supported on this device or OS version.
    /// </summary>
    SdkUnavailable = 1,

    /// <summary>
    /// Android only: Health Connect provider needs to be updated.
    /// Show your custom UI, then call <see cref="Services.IHealthService.OpenStorePageOfHealthProvider"/> to open the store.
    /// </summary>
    SdkUnavailableProviderUpdateRequired = 2,

    /// <summary>
    /// An error occurred while retrieving the list of permissions that have already been granted.
    /// </summary>
    ProblemWhileFetchingAlreadyGrantedPermissions,
    /// <summary>
    /// An error occurred during the permission grant flow.
    /// </summary>
    ProblemWhileGrantingPermissions,
    /// <summary>
    /// The user did not grant all of the requested permissions.
    /// </summary>
    MissingPermissions,
}
