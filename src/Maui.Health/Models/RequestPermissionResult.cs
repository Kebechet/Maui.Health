using Maui.Health.Enums.Errors;

namespace Maui.Health.Models;

/// <summary>
/// Result of a permission request, including any denied permissions.
/// <para>
/// <b>iOS limitation:</b> Apple does not expose read permission status. On iOS, <see cref="IsSuccess"/> only reflects
/// write permission grants and SDK errors. If a user denies read access, iOS does not report it as a failure —
/// the data simply appears empty. Use <see cref="Services.IHealthService.GetPermissionStatuses"/> for a more detailed
/// per-permission breakdown (which explicitly returns <see cref="Enums.HealthPermissionStatus.NotDetermined"/> for iOS read permissions).
/// </para>
/// </summary>
public class RequestPermissionResult : Result<RequestPermissionError>
{
    /// <summary>
    /// Whether all requested permissions were granted.
    /// On Android: accurately reflects both read and write permissions.
    /// On iOS: only reflects write permissions and SDK-level errors. Read permission denials are not detectable
    /// due to Apple's privacy design — <see cref="IsSuccess"/> may return <c>true</c> even if the user denied read access.
    /// </summary>
    public override bool IsSuccess => base.IsSuccess && !DeniedPermissions.Any();

    /// <summary>
    /// Platform-specific permission strings that were denied.
    /// These currently differ between Android and iOS - use just as debug info.
    /// TODO: Use platform-agnostic list of HealthPermissionDto instead of string as a return value
    /// </summary>
    public IList<string> DeniedPermissions { get; set; } = [];
    //public IList<HealthPermissionDto> DeniedPermissions { get; set; } = [];
}
