namespace Kebechet.Types.Health.Enums.Errors;

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
    /// Show your custom UI, then call <c>IHealthService.OpenStorePageOfHealthProvider</c> to open the store.
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
    /// The user did not grant all of the requested data-type permissions; <c>DeniedPermissions</c> lists them.
    /// Refused full-history or background access alone does not raise it.
    /// </summary>
    MissingPermissions,
    /// <summary>
    /// The request threw; the exception is on <see cref="Models.Result.ErrorException"/>.
    /// </summary>
    UnexpectedException,
    /// <summary>
    /// The caller's cancellation token fired before the user answered. A permission screen already showing cannot be
    /// withdrawn: it stays until the user answers, and the next request waits for it.
    /// </summary>
    Cancelled,
}
