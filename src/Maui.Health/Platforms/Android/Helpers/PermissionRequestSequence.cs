namespace Maui.Health.Platforms.Android.Helpers;

/// <summary>
/// The order in which a request's missing permissions are put in front of the user. Touches no Android API: the
/// launch itself is passed in, so the tests link it as source.
/// </summary>
internal static class PermissionRequestSequence
{
    /// <summary>
    /// Launches the data types first and the extras (full history, background read) in a launch of their own. On a
    /// Samsung A53 (Android 16) a request holding an extra the user had refused twice showed no screen at all, not
    /// even for a data type that was never refused. The extras are launched only once a data-type read is granted:
    /// they widen what the app can read, so without a read they grant nothing.
    /// </summary>
    /// <param name="grantedPermissions">What Health Connect reported as granted before the request; what the launches grant is added to it.</param>
    /// <param name="launch">Shows Health Connect's screen for the given permissions and returns what is granted afterwards and whether the screen showed.</param>
    /// <returns>What the launches granted, and whether any of them showed the screen.</returns>
    internal static async Task<(HashSet<string> NewlyGrantedPermissions, bool IsPermissionScreenShown)> Run(
        IEnumerable<string> missingPermissions,
        HashSet<string> grantedPermissions,
        Func<List<string>, Task<(HashSet<string> GrantedPermissions, bool IsPermissionScreenShown)>> launch)
    {
        var (missingDataTypePermissions, missingExtraPermissions) = HealthConnectPermissions.SplitByScreen(missingPermissions);
        var newlyGrantedPermissions = new HashSet<string>();
        var isPermissionScreenShown = false;

        if (missingDataTypePermissions.Any())
        {
            var (grantedDataTypePermissions, wasPermissionScreenShown) = await launch(missingDataTypePermissions);
            newlyGrantedPermissions.UnionWith(grantedDataTypePermissions);
            grantedPermissions.UnionWith(grantedDataTypePermissions);
            isPermissionScreenShown = wasPermissionScreenShown;
        }

        if (missingExtraPermissions.Any() && HealthConnectPermissions.HasAnyDataTypeReadPermission(grantedPermissions))
        {
            var (grantedExtraPermissions, wasPermissionScreenShown) = await launch(missingExtraPermissions);
            newlyGrantedPermissions.UnionWith(grantedExtraPermissions);
            grantedPermissions.UnionWith(grantedExtraPermissions);
            isPermissionScreenShown |= wasPermissionScreenShown;
        }

        return (newlyGrantedPermissions, isPermissionScreenShown);
    }
}
