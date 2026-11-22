using Kebechet.Types.Health.Enums;
using Kebechet.Types.Health.Models;
using Maui.Health.Services;
using Xunit;

namespace Maui.Health.Tests;

public class HealthPermissionDtoTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(5)]
    public void PermissionType_NeitherReadNorWriteOrUnknownBits_Throws(int permissionTypeValue)
    {
        // Arrange & Act
        var exception = Record.Exception(() => new HealthPermissionDto
        {
            HealthDataType = HealthDataType.Steps,
            PermissionType = (PermissionType)permissionTypeValue,
        });

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(exception);
    }

    [Fact]
    public void ToSingleAccessPermissions_ReadAndWrite_ReturnsAReadAndAWrite()
    {
        // Arrange
        var permission = new HealthPermissionDto
        {
            HealthDataType = HealthDataType.Weight,
            PermissionType = PermissionType.Read | PermissionType.Write,
        };

        // Act
        var singleAccessPermissions = permission
            .ToSingleAccessPermissions()
            .ToList();

        // Assert
        Assert.Equal(
            [PermissionType.Read, PermissionType.Write],
            singleAccessPermissions.Select(x => x.PermissionType));
        Assert.All(singleAccessPermissions, x => Assert.Equal(HealthDataType.Weight, x.HealthDataType));
    }

    [Fact]
    public void ToSingleAccessPermissions_WriteOnly_ReturnsOnlyTheWrite()
    {
        // Arrange
        var permission = new HealthPermissionDto
        {
            HealthDataType = HealthDataType.Weight,
            PermissionType = PermissionType.Write,
        };

        // Act
        var singleAccessPermission = Assert.Single(permission.ToSingleAccessPermissions());

        // Assert
        Assert.Equal(PermissionType.Write, singleAccessPermission.PermissionType);
    }

    [Fact]
    public void ToUnavailable_ReadWritePermission_ReportsReadAndWriteSeparately()
    {
        // Arrange
        var permission = new HealthPermissionDto
        {
            HealthDataType = HealthDataType.Weight,
            PermissionType = PermissionType.Read | PermissionType.Write,
        };

        // Act
        var statuses = PermissionStatuses.ToUnavailable([permission]);

        // Assert
        Assert.Equal(2, statuses.Count);
        Assert.All(statuses, x => Assert.Equal(HealthPermissionStatus.Unavailable, x.Status));
    }
}
