using Kebechet.Types.Health.Enums;
using Kebechet.Types.Health.Models;

namespace Maui.Health.Services;

/// <summary>
/// Status lists every platform builds the same way. Touches no platform API, so the tests link it as source.
/// </summary>
internal static class PermissionStatuses
{
    /// <summary>
    /// Every permission as <see cref="HealthPermissionStatus.Unavailable"/>, one entry per read and per write like a
    /// list the platform answered.
    /// </summary>
    internal static List<HealthPermissionStatusResult> ToUnavailable(IEnumerable<HealthPermissionDto> permissions)
    {
        return permissions
            .SelectMany(permission => permission.ToSingleAccessPermissions())
            .Select(permission => new HealthPermissionStatusResult
            {
                Permission = permission,
                Status = HealthPermissionStatus.Unavailable,
            })
            .ToList();
    }

    /// <summary>
    /// The requested permissions a request leaves without a grant. A status the platform could not read counts too:
    /// nothing confirmed it.
    /// </summary>
    internal static List<HealthPermissionDto> ToDenied(IEnumerable<HealthPermissionStatusResult> permissionStatuses)
    {
        return permissionStatuses
            .Where(x => x.Status is HealthPermissionStatus.Denied or HealthPermissionStatus.NotDetermined or HealthPermissionStatus.Unavailable)
            .Select(x => x.Permission)
            .ToList();
    }
}
