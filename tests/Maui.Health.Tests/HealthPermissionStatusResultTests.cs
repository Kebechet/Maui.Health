using Maui.Health.Enums;
using Maui.Health.Models;
using Xunit;

namespace Maui.Health.Tests;

public class HealthPermissionStatusResultTests
{
    [Fact]
    public void Constructor_WithRequiredProperties_SetsCorrectly()
    {
        // Arrange
        var permission = new HealthPermissionDto
        {
            HealthDataType = HealthDataType.Steps,
            PermissionType = PermissionType.Read
        };

        // Act
        var result = new HealthPermissionStatusResult
        {
            Permission = permission,
            Status = HealthPermissionStatus.Granted
        };

        // Assert
        Assert.Equal(permission, result.Permission);
        Assert.Equal(HealthPermissionStatus.Granted, result.Status);
    }

    [Theory]
    [InlineData(HealthPermissionStatus.Granted)]
    [InlineData(HealthPermissionStatus.Denied)]
    [InlineData(HealthPermissionStatus.NotDetermined)]
    public void Status_AllEnumValues_AreValid(HealthPermissionStatus status)
    {
        // Arrange
        var permission = new HealthPermissionDto
        {
            HealthDataType = HealthDataType.Weight,
            PermissionType = PermissionType.Write
        };

        // Act
        var result = new HealthPermissionStatusResult
        {
            Permission = permission,
            Status = status
        };

        // Assert
        Assert.Equal(status, result.Status);
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        var permission = new HealthPermissionDto
        {
            HealthDataType = HealthDataType.HeartRate,
            PermissionType = PermissionType.Read
        };

        // Act
        var result1 = new HealthPermissionStatusResult
        {
            Permission = permission,
            Status = HealthPermissionStatus.NotDetermined
        };
        var result2 = new HealthPermissionStatusResult
        {
            Permission = permission,
            Status = HealthPermissionStatus.NotDetermined
        };

        // Assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void RecordEquality_DifferentStatus_AreNotEqual()
    {
        // Arrange
        var permission = new HealthPermissionDto
        {
            HealthDataType = HealthDataType.Steps,
            PermissionType = PermissionType.Read
        };

        // Act
        var result1 = new HealthPermissionStatusResult
        {
            Permission = permission,
            Status = HealthPermissionStatus.Granted
        };
        var result2 = new HealthPermissionStatusResult
        {
            Permission = permission,
            Status = HealthPermissionStatus.Denied
        };

        // Assert
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void RecordEquality_DifferentPermission_AreNotEqual()
    {
        // Arrange
        var permission1 = new HealthPermissionDto
        {
            HealthDataType = HealthDataType.Steps,
            PermissionType = PermissionType.Read
        };
        var permission2 = new HealthPermissionDto
        {
            HealthDataType = HealthDataType.Weight,
            PermissionType = PermissionType.Read
        };

        // Act
        var result1 = new HealthPermissionStatusResult
        {
            Permission = permission1,
            Status = HealthPermissionStatus.Granted
        };
        var result2 = new HealthPermissionStatusResult
        {
            Permission = permission2,
            Status = HealthPermissionStatus.Granted
        };

        // Assert
        Assert.NotEqual(result1, result2);
    }
}
