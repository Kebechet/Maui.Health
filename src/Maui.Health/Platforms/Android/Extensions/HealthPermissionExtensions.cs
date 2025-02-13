using Maui.Health.Enums;
using Maui.Health.Models;

namespace Maui.Health.Platforms.Android.Extensions;

internal static class HealthPermissionExtensions
{
    private static readonly string _permissionsPrefix = "android.permission.health.";

    internal static List<string> ToStrings(this HealthPermissionDto healthPermission)
    {
        var partialPermissionNames = new List<string>();

        var permissionNameWithoutPrefix = healthPermission.HealthDataType.ToString().ToScreamingSnakeCase();

        if (healthPermission.PermissionType.HasFlag(PermissionType.Read))
        {
            partialPermissionNames.Add($"READ_{permissionNameWithoutPrefix}");
        }

        if (healthPermission.PermissionType.HasFlag(PermissionType.Write))
        {
            partialPermissionNames.Add($"WRITE_{permissionNameWithoutPrefix}");
        }

        return partialPermissionNames
            .Select(partialPermissionName => $"{_permissionsPrefix}{partialPermissionName}")
            .ToList();
    }
}
