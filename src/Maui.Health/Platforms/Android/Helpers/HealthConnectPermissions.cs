using Kebechet.Types.Health.Enums;
using Kebechet.Types.Health.Enums.Errors;
using Kebechet.Types.Health.Models;
using Maui.Health.Platforms.Android.Extensions;
using Maui.Health.Services;

namespace Maui.Health.Platforms.Android.Helpers;

/// <summary>
/// Turns the set of permission strings Health Connect reports as granted into statuses and request results.
/// Touches no Android API, so the tests link it as source.
/// </summary>
internal static class HealthConnectPermissions
{
    /// <summary>
    /// One status per read and per write: a permission requested as read and write is reported as two.
    /// Health Connect only tells which permissions are granted, so anything else is <see cref="HealthPermissionStatus.Denied"/>.
    /// </summary>
    internal static List<HealthPermissionStatusResult> ToStatuses(
        IEnumerable<HealthPermissionDto> permissions,
        IReadOnlySet<string> grantedPermissions)
    {
        return permissions
            .SelectMany(permission => permission.ToSingleAccessPermissions())
            .Select(permission => new HealthPermissionStatusResult
            {
                Permission = permission,
                Status = ToStatus(grantedPermissions, permission.ToStrings().Single()),
            })
            .ToList();
    }

    /// <summary>
    /// The outcome of a request once the user has answered. Only the requested data types decide it: full-history and
    /// background access are asked for on a screen of their own, and refusing them must not undo the data types the
    /// user allowed.
    /// </summary>
    internal static RequestPermissionResult ToRequestResult(
        IList<HealthPermissionDto> requestedPermissions,
        IReadOnlySet<string> grantedPermissions,
        bool isFullHistoryReadRequested,
        bool isBackgroundReadRequested,
        bool isPermissionScreenShown)
    {
        var permissionStatuses = ToStatuses(requestedPermissions, grantedPermissions);
        var deniedPermissions = PermissionStatuses.ToDenied(permissionStatuses);

        return new RequestPermissionResult
        {
            Error = deniedPermissions.Any()
                ? RequestPermissionError.MissingPermissions
                : null,
            PermissionStatuses = permissionStatuses,
            DeniedPermissions = deniedPermissions,
            FullHistoryReadStatus = isFullHistoryReadRequested
                ? ToStatus(grantedPermissions, AndroidConstant.FullHistoryReadPermission)
                : null,
            BackgroundReadStatus = isBackgroundReadRequested
                ? ToStatus(grantedPermissions, AndroidConstant.BackgroundReadPermission)
                : null,
            IsPermissionScreenShown = isPermissionScreenShown,
        };
    }

    /// <summary>
    /// Splits missing permissions into the data types and the extras (full history, background read), which Health
    /// Connect asks for on a screen of their own and which are therefore requested apart.
    /// </summary>
    internal static (List<string> DataTypePermissions, List<string> ExtraPermissions) SplitByScreen(IEnumerable<string> missingPermissions)
    {
        var missingPermissionList = missingPermissions.ToList();

        return (
            missingPermissionList
                .Where(x => !IsExtraPermission(x))
                .ToList(),
            missingPermissionList
                .Where(IsExtraPermission)
                .ToList());
    }

    /// <summary>
    /// Whether the app holds a permission for any data type; full-history and background access do not count.
    /// </summary>
    internal static bool HasAnyDataTypePermission(IEnumerable<string> grantedPermissions)
    {
        return grantedPermissions.Any(x => x.StartsWith(AndroidConstant.HealthPermissionPrefix, StringComparison.Ordinal) &&
            !IsExtraPermission(x));
    }

    private static bool IsExtraPermission(string androidPermission)
    {
        return androidPermission is AndroidConstant.FullHistoryReadPermission or AndroidConstant.BackgroundReadPermission;
    }

    private static HealthPermissionStatus ToStatus(IReadOnlySet<string> grantedPermissions, string androidPermission)
    {
        return grantedPermissions.Contains(androidPermission)
            ? HealthPermissionStatus.Granted
            : HealthPermissionStatus.Denied;
    }
}
