using Maui.Health.Enums;
using Maui.Health.Models;

namespace Maui.Health.Platforms.Android.Extensions;

internal static class HealthPermissionExtensions
{
    private static readonly string _permissionsPrefix = "android.permission.health.";
    private const string _exercisePermissionName = "EXERCISE";

    internal static List<string> ToStrings(this HealthPermissionDto healthPermission)
    {
        var partialPermissionNames = new List<string>();

        var permissionNameWithoutPrefix = healthPermission.HealthDataType.ToString().ToScreamingSnakeCase();

        // Special case: ExerciseSession maps to EXERCISE in Android Health Connect
        if (healthPermission.HealthDataType == HealthDataType.ExerciseSession)
        {
            permissionNameWithoutPrefix = _exercisePermissionName;
        }

        switch (healthPermission.PermissionType)
        {
            case PermissionType.Read:
                partialPermissionNames.Add($"READ_{permissionNameWithoutPrefix}");
                break;
            case PermissionType.Write:
                partialPermissionNames.Add($"WRITE_{permissionNameWithoutPrefix}");
                break;
            case PermissionType.ReadWrite:
                partialPermissionNames.Add($"READ_{permissionNameWithoutPrefix}");
                partialPermissionNames.Add($"WRITE_{permissionNameWithoutPrefix}");
                break;
        }

        return partialPermissionNames
            .Select(partialPermissionName => $"{_permissionsPrefix}{partialPermissionName}")
            .ToList();
    }
}
