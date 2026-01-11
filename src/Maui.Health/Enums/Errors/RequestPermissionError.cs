namespace Maui.Health.Enums.Errors;

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
