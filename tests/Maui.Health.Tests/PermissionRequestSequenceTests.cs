using Maui.Health.Platforms.Android;
using Maui.Health.Platforms.Android.Helpers;
using Xunit;

namespace Maui.Health.Tests;

public class PermissionRequestSequenceTests
{
    private const string ReadSteps = "android.permission.health.READ_STEPS";
    private const string WriteWeight = "android.permission.health.WRITE_WEIGHT";

    private readonly List<List<string>> _launches = [];

    private Func<List<string>, Task<(HashSet<string> GrantedPermissions, bool IsPermissionScreenShown)>> CreateLaunch(
        Func<List<string>, HashSet<string>> grant,
        bool isPermissionScreenShown = true)
    {
        return permissions =>
        {
            _launches.Add(permissions);
            return Task.FromResult((grant(permissions), isPermissionScreenShown));
        };
    }

    /// <summary>
    /// A request holding an extra the user refused twice showed no screen at all, not even for a data type, so the
    /// extras get a launch of their own after the data types.
    /// </summary>
    [Fact]
    public async Task Run_DataTypesAndExtrasMissing_LaunchesTheDataTypesFirstAndTheExtrasApart()
    {
        // Arrange
        List<string> missingPermissions = [ReadSteps, AndroidConstant.FullHistoryReadPermission, AndroidConstant.BackgroundReadPermission];
        var grantedPermissions = new HashSet<string>();

        // Act
        var (newlyGrantedPermissions, isPermissionScreenShown) = await PermissionRequestSequence.Run(
            missingPermissions,
            grantedPermissions,
            CreateLaunch(permissions => permissions.ToHashSet()));

        // Assert
        Assert.Equal(2, _launches.Count);
        Assert.Equal([ReadSteps], _launches[0]);
        Assert.Equal([AndroidConstant.FullHistoryReadPermission, AndroidConstant.BackgroundReadPermission], _launches[1]);
        Assert.Equal(missingPermissions.ToHashSet(), newlyGrantedPermissions);
        Assert.Equal(missingPermissions.ToHashSet(), grantedPermissions);
        Assert.True(isPermissionScreenShown);
    }

    [Fact]
    public async Task Run_DataTypeRefused_SkipsTheExtras()
    {
        // Arrange
        List<string> missingPermissions = [ReadSteps, AndroidConstant.BackgroundReadPermission];

        // Act
        var (newlyGrantedPermissions, _) = await PermissionRequestSequence.Run(
            missingPermissions,
            [],
            CreateLaunch(_ => []));

        // Assert
        var launch = Assert.Single(_launches);
        Assert.Equal([ReadSteps], launch);
        Assert.Empty(newlyGrantedPermissions);
    }

    [Fact]
    public async Task Run_OnlyAWriteGranted_SkipsTheExtras()
    {
        // Arrange
        List<string> missingPermissions = [WriteWeight, AndroidConstant.FullHistoryReadPermission];

        // Act
        await PermissionRequestSequence.Run(
            missingPermissions,
            [],
            CreateLaunch(permissions => permissions.ToHashSet()));

        // Assert
        var launch = Assert.Single(_launches);
        Assert.Equal([WriteWeight], launch);
    }

    [Fact]
    public async Task Run_OnlyExtrasMissingAndAReadHeld_LaunchesOnlyTheExtras()
    {
        // Arrange
        List<string> missingPermissions = [AndroidConstant.BackgroundReadPermission];
        var grantedPermissions = new HashSet<string> { ReadSteps };

        // Act
        await PermissionRequestSequence.Run(
            missingPermissions,
            grantedPermissions,
            CreateLaunch(permissions => permissions.ToHashSet()));

        // Assert
        var launch = Assert.Single(_launches);
        Assert.Equal([AndroidConstant.BackgroundReadPermission], launch);
        Assert.Contains(AndroidConstant.BackgroundReadPermission, grantedPermissions);
    }

    [Fact]
    public async Task Run_OnlyTheExtrasLaunchShowsTheScreen_ReportsTheScreenShown()
    {
        // Arrange
        List<string> missingPermissions = [ReadSteps, AndroidConstant.BackgroundReadPermission];
        var isFirstLaunch = true;

        // Act
        var (_, isPermissionScreenShown) = await PermissionRequestSequence.Run(
            missingPermissions,
            [],
            permissions =>
            {
                var wasFirstLaunch = isFirstLaunch;
                isFirstLaunch = false;
                return Task.FromResult((permissions.ToHashSet(), !wasFirstLaunch));
            });

        // Assert
        Assert.True(isPermissionScreenShown);
    }
}
