using Kebechet.Types.Health.Enums.Errors;
using Kebechet.Types.Health.Models;
using Maui.Health.Services;
using Xunit;

namespace Maui.Health.Tests;

public class PermissionRequestErrorsTests
{
    [Fact]
    public void ToReadResult_Refused_IsPermissionDenied()
    {
        // Arrange
        var requestPermissionResult = new RequestPermissionResult { Error = RequestPermissionError.MissingPermissions };

        // Act
        var readResult = PermissionRequestErrors.ToReadResult<AggregatedReadResult>(requestPermissionResult);

        // Assert
        Assert.Equal(ReadHealthDataError.PermissionDenied, readResult.Error);
        Assert.Null(readResult.ErrorException);
    }

    /// <summary>
    /// A sync cancelled while its permission request waited in the queue must not read as the user refusing.
    /// </summary>
    [Fact]
    public void ToReadResult_Cancelled_IsCancelled()
    {
        // Arrange
        var requestPermissionResult = new RequestPermissionResult { Error = RequestPermissionError.Cancelled };

        // Act
        var readResult = PermissionRequestErrors.ToReadResult<HealthDataReadResult<Kebechet.Types.Health.Models.Metrics.StepsDto>>(requestPermissionResult);

        // Assert
        Assert.Equal(ReadHealthDataError.Cancelled, readResult.Error);
    }

    [Fact]
    public void ToReadResult_OtherFailure_CarriesAnExceptionAndNoTypedError()
    {
        // Arrange
        var requestPermissionResult = new RequestPermissionResult { Error = RequestPermissionError.UnexpectedException };

        // Act
        var readResult = PermissionRequestErrors.ToReadResult<AggregatedIntervalReadResult>(requestPermissionResult);

        // Assert
        Assert.Null(readResult.Error);
        Assert.NotNull(readResult.ErrorException);
    }

    /// <summary>
    /// A read that has to ask first on a device with no health platform reports that, as a write does, instead of an
    /// exception only.
    /// </summary>
    [Theory]
    [InlineData(RequestPermissionError.SdkUnavailable)]
    [InlineData(RequestPermissionError.SdkUnavailableProviderUpdateRequired)]
    public void ToReadResult_SdkUnavailable_IsSdkUnavailable(RequestPermissionError requestPermissionError)
    {
        // Arrange
        var requestPermissionResult = new RequestPermissionResult { Error = requestPermissionError };

        // Act
        var readResult = PermissionRequestErrors.ToReadResult<AggregatedReadResult>(requestPermissionResult);

        // Assert
        Assert.Equal(ReadHealthDataError.SdkUnavailable, readResult.Error);
    }

    [Theory]
    [InlineData(RequestPermissionError.MissingPermissions, WriteHealthDataError.PermissionDenied)]
    [InlineData(RequestPermissionError.Cancelled, WriteHealthDataError.Cancelled)]
    [InlineData(RequestPermissionError.SdkUnavailable, WriteHealthDataError.SdkUnavailable)]
    [InlineData(RequestPermissionError.SdkUnavailableProviderUpdateRequired, WriteHealthDataError.SdkUnavailable)]
    [InlineData(RequestPermissionError.UnexpectedException, WriteHealthDataError.UnexpectedException)]
    [InlineData(RequestPermissionError.ProblemWhileGrantingPermissions, WriteHealthDataError.UnexpectedException)]
    public void ToWriteError_RequestError_MapsOnlyARefusalToPermissionDenied(RequestPermissionError requestPermissionError, WriteHealthDataError expectedWriteError)
    {
        // Arrange & Act
        var writeError = PermissionRequestErrors.ToWriteError(requestPermissionError);

        // Assert
        Assert.Equal(expectedWriteError, writeError);
    }

    [Theory]
    [InlineData(RequestPermissionError.MissingPermissions, UpdateHealthDataError.PermissionDenied)]
    [InlineData(RequestPermissionError.Cancelled, UpdateHealthDataError.Cancelled)]
    [InlineData(RequestPermissionError.SdkUnavailable, UpdateHealthDataError.SdkUnavailable)]
    [InlineData(RequestPermissionError.UnexpectedException, UpdateHealthDataError.UnexpectedException)]
    public void ToUpdateError_RequestError_MapsOnlyARefusalToPermissionDenied(RequestPermissionError requestPermissionError, UpdateHealthDataError expectedUpdateError)
    {
        // Arrange & Act
        var updateError = PermissionRequestErrors.ToUpdateError(requestPermissionError);

        // Assert
        Assert.Equal(expectedUpdateError, updateError);
    }
}
