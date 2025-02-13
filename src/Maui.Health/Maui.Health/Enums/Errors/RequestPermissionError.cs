namespace Maui.Health.Enums.Errors;

public enum RequestPermissionError
{
    IsNotSupported,
    ProblemWhileFetchingAlreadyGrantedPermissions,
    ProblemWhileGrantingPermissions,
    MissingPermissions
}
