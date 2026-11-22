using Kebechet.Types.Health.Enums;
using Kebechet.Types.Health.Enums.Errors;
using Kebechet.Types.Health.Models;
using Maui.Health.Platforms.iOS.Helpers;
using Xunit;

namespace Maui.Health.Tests;

public class HealthKitPermissionsTests
{
    private static readonly HealthPermissionDto _readSteps = new()
    {
        HealthDataType = HealthDataType.Steps,
        PermissionType = PermissionType.Read,
    };

    private static readonly HealthPermissionDto _readWriteWeight = new()
    {
        HealthDataType = HealthDataType.Weight,
        PermissionType = PermissionType.Read | PermissionType.Write,
    };

    [Fact]
    public void ToStatuses_ReadWritePermission_ReportsAnUndisclosedReadAndTheWriteStatusSeparately()
    {
        // Arrange & Act
        var statuses = HealthKitPermissions.ToStatuses([_readWriteWeight], _ => HealthPermissionStatus.Denied);

        // Assert
        Assert.Equal(2, statuses.Count);
        Assert.Contains(statuses, x => x.Permission.PermissionType == PermissionType.Read &&
            x.Status == HealthPermissionStatus.Undisclosed);
        Assert.Contains(statuses, x => x.Permission.PermissionType == PermissionType.Write &&
            x.Status == HealthPermissionStatus.Denied);
    }

    /// <summary>
    /// The user allows reading steps but turns off the weight write: only the write is denied.
    /// </summary>
    [Fact]
    public void ToRequestResult_WriteTurnedOff_DeniesOnlyTheWrite()
    {
        // Arrange & Act
        var result = HealthKitPermissions.ToRequestResult(
            [_readSteps, _readWriteWeight],
            _ => HealthPermissionStatus.Denied,
            isPermissionScreenShown: true);

        // Assert
        Assert.Equal(RequestPermissionError.MissingPermissions, result.Error);
        var deniedPermission = Assert.Single(result.DeniedPermissions);
        Assert.Equal(HealthDataType.Weight, deniedPermission.HealthDataType);
        Assert.Equal(PermissionType.Write, deniedPermission.PermissionType);
        Assert.Contains(result.PermissionStatuses, x => x.Permission.HealthDataType == HealthDataType.Steps &&
            x.Status == HealthPermissionStatus.Undisclosed);
        Assert.True(result.IsPermissionScreenShown);
    }

    [Theory]
    [InlineData(HealthPermissionStatus.NotDetermined)]
    [InlineData(HealthPermissionStatus.Unavailable)]
    public void ToRequestResult_WriteNotConfirmed_DeniesTheWrite(HealthPermissionStatus writeStatus)
    {
        // Arrange & Act
        var result = HealthKitPermissions.ToRequestResult([_readWriteWeight], _ => writeStatus, isPermissionScreenShown: false);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Single(result.DeniedPermissions);
    }

    [Fact]
    public void ToRequestResult_ReadsOnly_IsSuccess()
    {
        // Arrange & Act
        var result = HealthKitPermissions.ToRequestResult([_readSteps], _ => HealthPermissionStatus.Denied, isPermissionScreenShown: false);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.DeniedPermissions);
    }
}
