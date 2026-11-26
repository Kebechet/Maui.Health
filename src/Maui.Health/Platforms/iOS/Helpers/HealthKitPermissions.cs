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
    /// A data type <paramref name="isSupported"/> rejects has no HealthKit counterpart: its read and its write are
    /// <see cref="HealthPermissionStatus.Unavailable"/> and the other permissions are not affected.
    /// https://developer.apple.com/documentation/healthkit/hkhealthstore/authorizationstatus(for:)
    /// </summary>
    internal static List<HealthPermissionStatusResult> ToStatuses(
        IEnumerable<HealthPermissionDto> permissions,
        Func<HealthDataType, bool> isSupported,
        Func<HealthDataType, HealthPermissionStatus> getWriteStatus)
    {
        return permissions
            .SelectMany(permission => permission.ToSingleAccessPermissions())
            .Select(permission => new HealthPermissionStatusResult
            {
                Permission = permission,
                Status = ToStatus(permission, isSupported, getWriteStatus),
            })
            .ToList();
    }

    /// <summary>
    /// The outcome of an answered request. Only writes can be denied: a refused read is never disclosed.
    /// </summary>
    internal static RequestPermissionResult ToRequestResult(
        IList<HealthPermissionDto> requestedPermissions,
        Func<HealthDataType, bool> isSupported,
        Func<HealthDataType, HealthPermissionStatus> getWriteStatus,
        bool isPermissionScreenShown)
    {
        var permissionStatuses = ToStatuses(requestedPermissions, isSupported, getWriteStatus);
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

    private static HealthPermissionStatus ToStatus(
        HealthPermissionDto singleAccessPermission,
        Func<HealthDataType, bool> isSupported,
        Func<HealthDataType, HealthPermissionStatus> getWriteStatus)
    {
        if (!isSupported(singleAccessPermission.HealthDataType))
        {
            return HealthPermissionStatus.Unavailable;
        }

        return singleAccessPermission.PermissionType == PermissionType.Read
            ? HealthPermissionStatus.Undisclosed
            : getWriteStatus(singleAccessPermission.HealthDataType);
    }
}
