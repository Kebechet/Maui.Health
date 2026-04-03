using Maui.Health.Enums.Errors;
using Maui.Health.Models;
using Xunit;

namespace Maui.Health.Tests;

public class ResultTests
{
    [Fact]
    public void Result_Default_IsSuccess()
    {
        // Arrange & Act
        var result = new Result<RequestPermissionError>();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsError);
    }

    [Fact]
    public void Result_WithError_IsNotSuccess()
    {
        // Arrange & Act
        var result = new Result<RequestPermissionError>
        {
            Error = RequestPermissionError.SdkUnavailable
        };

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsError);
    }

    [Fact]
    public void Result_WithException_IsNotSuccess()
    {
        // Arrange & Act
        var result = new Result<RequestPermissionError>
        {
            ErrorException = new InvalidOperationException("test")
        };

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsError);
    }

    [Fact]
    public void RequestPermissionResult_Default_IsSuccess()
    {
        // Arrange & Act
        var result = new RequestPermissionResult();

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void RequestPermissionResult_WithDeniedPermissions_IsNotSuccess()
    {
        // Arrange & Act
        var result = new RequestPermissionResult
        {
            DeniedPermissions = ["com.health.steps"]
        };

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsError);
    }

    [Fact]
    public void RequestPermissionResult_EmptyDeniedPermissions_IsSuccess()
    {
        // Arrange & Act
        var result = new RequestPermissionResult
        {
            DeniedPermissions = []
        };

        // Assert
        Assert.True(result.IsSuccess);
    }
}
