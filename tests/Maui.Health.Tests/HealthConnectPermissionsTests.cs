using Kebechet.Types.Health.Enums;
using Kebechet.Types.Health.Enums.Errors;
using Kebechet.Types.Health.Models;
using Maui.Health.Platforms.Android;
using Maui.Health.Platforms.Android.Helpers;
using Xunit;

namespace Maui.Health.Tests;

public class HealthConnectPermissionsTests
{
    private const string ReadSteps = "android.permission.health.READ_STEPS";
    private const string ReadWeight = "android.permission.health.READ_WEIGHT";
    private const string WriteWeight = "android.permission.health.WRITE_WEIGHT";

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
    public void ToRequestResult_DataTypesGrantedAndExtrasRefused_IsSuccessWithTheExtrasReportedApart()
    {
        // Arrange
        var grantedPermissions = new HashSet<string> { ReadSteps };

        // Act
        var result = HealthConnectPermissions.ToRequestResult(
            [_readSteps],
            grantedPermissions,
            isFullHistoryReadRequested: true,
            isBackgroundReadRequested: true,
            isPermissionScreenShown: true);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Empty(result.DeniedPermissions);
        Assert.Equal(HealthPermissionStatus.Denied, result.FullHistoryReadStatus);
        Assert.Equal(HealthPermissionStatus.Denied, result.BackgroundReadStatus);
        Assert.True(result.IsPermissionScreenShown);
    }

    [Fact]
    public void ToRequestResult_ExtrasGranted_ReportsThemGranted()
    {
        // Arrange
        var grantedPermissions = new HashSet<string>
        {
            ReadSteps,
            AndroidConstant.FullHistoryReadPermission,
            AndroidConstant.BackgroundReadPermission,
        };

        // Act
        var result = HealthConnectPermissions.ToRequestResult(
            [_readSteps],
            grantedPermissions,
            isFullHistoryReadRequested: true,
            isBackgroundReadRequested: true,
            isPermissionScreenShown: false);

        // Assert
        Assert.Equal(HealthPermissionStatus.Granted, result.FullHistoryReadStatus);
        Assert.Equal(HealthPermissionStatus.Granted, result.BackgroundReadStatus);
        Assert.False(result.IsPermissionScreenShown);
    }

    [Fact]
    public void ToRequestResult_ExtrasNotRequested_LeavesTheirStatusesNull()
    {
        // Arrange
        var grantedPermissions = new HashSet<string> { ReadSteps };

        // Act
        var result = HealthConnectPermissions.ToRequestResult(
            [_readSteps],
            grantedPermissions,
            isFullHistoryReadRequested: false,
            isBackgroundReadRequested: false,
            isPermissionScreenShown: false);

        // Assert
        Assert.Null(result.FullHistoryReadStatus);
        Assert.Null(result.BackgroundReadStatus);
    }

    [Fact]
    public void ToRequestResult_OneDataTypeRefused_ReportsExactlyThatPermissionAsDenied()
    {
        // Arrange
        var grantedPermissions = new HashSet<string> { ReadSteps, ReadWeight };

        // Act
        var result = HealthConnectPermissions.ToRequestResult(
            [_readSteps, _readWriteWeight],
            grantedPermissions,
            isFullHistoryReadRequested: false,
            isBackgroundReadRequested: false,
            isPermissionScreenShown: true);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(RequestPermissionError.MissingPermissions, result.Error);
        var deniedPermission = Assert.Single(result.DeniedPermissions);
        Assert.Equal(HealthDataType.Weight, deniedPermission.HealthDataType);
        Assert.Equal(PermissionType.Write, deniedPermission.PermissionType);
    }

    [Fact]
    public void ToStatuses_WriteOnlyPermission_ReportsOnlyTheWrite()
    {
        // Arrange
        var writeWeight = new HealthPermissionDto
        {
            HealthDataType = HealthDataType.Weight,
            PermissionType = PermissionType.Write,
        };
        var grantedPermissions = new HashSet<string> { WriteWeight };

        // Act
        var statuses = HealthConnectPermissions.ToStatuses([writeWeight], grantedPermissions);

        // Assert
        var status = Assert.Single(statuses);
        Assert.Equal(PermissionType.Write, status.Permission.PermissionType);
        Assert.Equal(HealthPermissionStatus.Granted, status.Status);
    }

    [Fact]
    public void ToStatuses_ReadWritePermission_ReportsReadAndWriteSeparately()
    {
        // Arrange
        var grantedPermissions = new HashSet<string> { ReadWeight };

        // Act
        var statuses = HealthConnectPermissions.ToStatuses([_readSteps, _readWriteWeight], grantedPermissions);

        // Assert
        Assert.Equal(3, statuses.Count);
        Assert.Contains(statuses, x => x.Permission.HealthDataType == HealthDataType.Steps &&
            x.Permission.PermissionType == PermissionType.Read &&
            x.Status == HealthPermissionStatus.Denied);
        Assert.Contains(statuses, x => x.Permission.HealthDataType == HealthDataType.Weight &&
            x.Permission.PermissionType == PermissionType.Read &&
            x.Status == HealthPermissionStatus.Granted);
        Assert.Contains(statuses, x => x.Permission.HealthDataType == HealthDataType.Weight &&
            x.Permission.PermissionType == PermissionType.Write &&
            x.Status == HealthPermissionStatus.Denied);
    }

    [Fact]
    public void ToRequestResult_Always_ReportsAStatusForEveryRequestedPermission()
    {
        // Arrange
        var grantedPermissions = new HashSet<string> { ReadSteps, ReadWeight, WriteWeight };

        // Act
        var result = HealthConnectPermissions.ToRequestResult(
            [_readSteps, _readWriteWeight],
            grantedPermissions,
            isFullHistoryReadRequested: false,
            isBackgroundReadRequested: false,
            isPermissionScreenShown: false);

        // Assert
        Assert.Equal(3, result.PermissionStatuses.Count);
        Assert.All(result.PermissionStatuses, x => Assert.Equal(HealthPermissionStatus.Granted, x.Status));
    }

    [Fact]
    public void ToUnavailableStatuses_Always_ReportsEveryPermissionUnavailable()
    {
        // Arrange & Act
        var statuses = HealthConnectPermissions.ToUnavailableStatuses([_readSteps, _readWriteWeight]);

        // Assert
        Assert.Equal(2, statuses.Count);
        Assert.All(statuses, x => Assert.Equal(HealthPermissionStatus.Unavailable, x.Status));
    }
}
