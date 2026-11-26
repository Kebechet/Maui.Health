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
        var statuses = HealthKitPermissions.ToStatuses([_readWriteWeight], _ => true, _ => HealthPermissionStatus.Denied);

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
            _ => true,
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
        var result = HealthKitPermissions.ToRequestResult([_readWriteWeight], _ => true, _ => writeStatus, isPermissionScreenShown: false);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Single(result.DeniedPermissions);
    }

    [Fact]
    public void ToRequestResult_ReadsOnly_IsSuccess()
    {
        // Arrange & Act
        var result = HealthKitPermissions.ToRequestResult([_readSteps], _ => true, _ => HealthPermissionStatus.Denied, isPermissionScreenShown: false);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.DeniedPermissions);
    }

    /// <summary>
    /// A data type with no HealthKit counterpart (Health Connect has types HealthKit lacks) is reported on its own, and
    /// the permissions HealthKit does know keep the status HealthKit gave them.
    /// </summary>
    [Fact]
    public void ToStatuses_TypeHealthKitDoesNotHave_IsUnavailableForReadAndWriteOnly()
    {
        // Arrange
        var readWriteBloodPressure = new HealthPermissionDto
        {
            HealthDataType = HealthDataType.BloodPressure,
            PermissionType = PermissionType.Read | PermissionType.Write,
        };

        // Act
        var statuses = HealthKitPermissions.ToStatuses(
            [_readSteps, readWriteBloodPressure],
            healthDataType => healthDataType != HealthDataType.BloodPressure,
            _ => HealthPermissionStatus.Granted);

        // Assert
        Assert.Equal(3, statuses.Count);
        Assert.Single(statuses, x => x.Permission.HealthDataType == HealthDataType.Steps &&
            x.Status == HealthPermissionStatus.Undisclosed);
        Assert.Equal(2, statuses.Count(x => x.Permission.HealthDataType == HealthDataType.BloodPressure &&
            x.Status == HealthPermissionStatus.Unavailable));
    }

    [Fact]
    public void ToRequestResult_TypeHealthKitDoesNotHave_DeniesOnlyThatType()
    {
        // Arrange
        var readBloodPressure = new HealthPermissionDto
        {
            HealthDataType = HealthDataType.BloodPressure,
            PermissionType = PermissionType.Read,
        };

        // Act
        var result = HealthKitPermissions.ToRequestResult(
            [_readSteps, readBloodPressure],
            healthDataType => healthDataType != HealthDataType.BloodPressure,
            _ => HealthPermissionStatus.Granted,
            isPermissionScreenShown: false);

        // Assert
        Assert.Equal(RequestPermissionError.MissingPermissions, result.Error);
        var deniedPermission = Assert.Single(result.DeniedPermissions);
        Assert.Equal(HealthDataType.BloodPressure, deniedPermission.HealthDataType);
        Assert.Contains(result.PermissionStatuses, x => x.Permission.HealthDataType == HealthDataType.Steps &&
            x.Status == HealthPermissionStatus.Undisclosed);
    }

    /// <summary>
    /// HealthKit rejects a request that names no type, so a request of only types it does not have is answered without
    /// asking it: every access is unavailable and denied, and no screen was shown.
    /// </summary>
    [Fact]
    public void ToRequestResult_OnlyTypesHealthKitDoesNotHave_DeniesEveryAccessAsUnavailable()
    {
        // Arrange
        var readWriteBloodPressure = new HealthPermissionDto
        {
            HealthDataType = HealthDataType.BloodPressure,
            PermissionType = PermissionType.Read | PermissionType.Write,
        };

        // Act
        var result = HealthKitPermissions.ToRequestResult(
            [readWriteBloodPressure],
            _ => false,
            _ => HealthPermissionStatus.Unavailable,
            isPermissionScreenShown: false);

        // Assert
        Assert.Equal(RequestPermissionError.MissingPermissions, result.Error);
        Assert.Equal(2, result.DeniedPermissions.Count);
        Assert.All(result.PermissionStatuses, x => Assert.Equal(HealthPermissionStatus.Unavailable, x.Status));
        Assert.False(result.IsPermissionScreenShown);
    }

    [Fact]
    public void ToRequestResult_NothingRequested_IsSuccessWithNoStatuses()
    {
        // Arrange & Act
        var result = HealthKitPermissions.ToRequestResult([], _ => false, _ => HealthPermissionStatus.Unavailable, isPermissionScreenShown: false);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.PermissionStatuses);
        Assert.Empty(result.DeniedPermissions);
    }
}
