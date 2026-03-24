using Maui.Health.Enums.Errors;

namespace Maui.Health.Models;

/// <summary>
/// Result of a permission request, including any denied permissions.
/// </summary>
public class RequestPermissionResult : Result<RequestPermissionError>
{
    /// <inheritdoc/>
    public override bool IsSuccess => base.IsSuccess && !DeniedPermissions.Any();
    /// <summary>
    /// These currently differ between Android and iOS - use just as an debug info.
    /// TODO: Use platform-agnostic list of HealthPermissionDto instead of string as a return value
    /// </summary>
    public IList<string> DeniedPermissions { get; set; } = [];
    //public IList<HealthPermissionDto> DeniedPermissions { get; set; } = [];
}
