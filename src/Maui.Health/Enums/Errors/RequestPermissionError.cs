namespace Maui.Health.Enums.Errors;

/// <summary>
/// Errors that can occur during health permission requests.
/// Note: Android Health Connect provider update scenarios are handled separately via
/// <see cref="SdkCheckError.SdkUnavailableProviderUpdateRequired"/> which automatically
/// opens the Play Store for the user to update the Health Connect app.
/// </summary>
public enum RequestPermissionError
{
    IsNotSupported,
    ProblemWhileFetchingAlreadyGrantedPermissions,
    ProblemWhileGrantingPermissions,
    MissingPermissions,
    /// <summary>
    /// Android only: Health Connect provider needs to be updated.
    /// Show your custom UI, then call <see cref="Services.IHealthService.OpenHealthStoreForUpdate"/> to open the store.
    /// </summary>
    AndroidSdkUnavailableProviderUpdateRequired
}
