using Kebechet.Types.Health.Enums;
using Kebechet.Types.Health.Enums.Errors;
using Kebechet.Types.Health.Models;
using Maui.Health.Services;

namespace Maui.Health.Platforms.iOS.Helpers;

/// <summary>
/// Turns HealthKit's authorization statuses into statuses and request results. Touches no HealthKit API: the caller
/// passes the write statuses in, so the tests link it as source.
/// </summary>
internal static class HealthKitPermissions
{
    /// <summary>
    /// One status per read and per write. HealthKit never discloses a read, so every read is
    /// <see cref="HealthPermissionStatus.Undisclosed"/>; a write gets the status <paramref name="getWriteStatus"/> reads.
    /// https://developer.apple.com/documentation/healthkit/hkhealthstore/authorizationstatus(for:)
    /// </summary>
    internal static List<HealthPermissionStatusResult> ToStatuses(
        IEnumerable<HealthPermissionDto> permissions,
        Func<HealthDataType, HealthPermissionStatus> getWriteStatus)
    {
        return permissions
            .SelectMany(permission => permission.ToSingleAccessPermissions())
            .Select(permission => new HealthPermissionStatusResult
            {
                Permission = permission,
                Status = permission.PermissionType == PermissionType.Read
                    ? HealthPermissionStatus.Undisclosed
                    : getWriteStatus(permission.HealthDataType),
            })
            .ToList();
    }

    /// <summary>
    /// The outcome of an answered request. Only writes can be denied: a refused read is never disclosed.
    /// </summary>
    internal static RequestPermissionResult ToRequestResult(
        IList<HealthPermissionDto> requestedPermissions,
        Func<HealthDataType, HealthPermissionStatus> getWriteStatus,
        bool isPermissionScreenShown)
    {
        var permissionStatuses = ToStatuses(requestedPermissions, getWriteStatus);
        var deniedPermissions = PermissionStatuses.ToDenied(permissionStatuses);

        return new RequestPermissionResult
        {
            Error = deniedPermissions.Any()
                ? RequestPermissionError.MissingPermissions
                : null,
            PermissionStatuses = permissionStatuses,
            DeniedPermissions = deniedPermissions,
            IsPermissionScreenShown = isPermissionScreenShown,
        };
    }
}
