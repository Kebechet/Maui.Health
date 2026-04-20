using Maui.Health.Enums.Errors;
using Maui.Health.Models;
using Xunit;

namespace Maui.Health.Tests;

public class UpdateHealthDataResultTests
{
    [Fact]
    public void Default_HasNoRecordIdAndReportsSuccess()
    {
        // Arrange
        // Act
        var result = new UpdateHealthDataResult();

        // Assert
        // A parameterless result with no Error/ErrorException is a success by Result<TError>.
        // Producers are expected to set RecordId on the success path and Error on failures;
        // the default is a degenerate success used in tests and stubs.
        Assert.True(result.IsSuccess);
        Assert.False(result.IsError);
        Assert.Null(result.RecordId);
        Assert.Null(result.Error);
        Assert.Null(result.ErrorException);
    }

    [Fact]
    public void WithRecordId_IsSuccessAndExposesId()
    {
        // Arrange
        // Act
        var result = new UpdateHealthDataResult { RecordId = "updated-id" };

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("updated-id", result.RecordId);
    }

    [Fact]
    public void WithError_IsErrorAndRecordIdNull()
    {
        // Arrange
        // Act
        var result = new UpdateHealthDataResult { Error = UpdateHealthDataError.PermissionDenied };

        // Assert
        Assert.True(result.IsError);
        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateHealthDataError.PermissionDenied, result.Error);
        Assert.Null(result.RecordId);
    }

    [Fact]
    public void WithLegacyRecordNotUpdatable_IsErrorAndSurfacesSpecificValue()
    {
        // Arrange
        // LegacyRecordNotUpdatable is the iOS-specific case where the existing sample carries
        // no HKMetadataKeySyncIdentifier, so HealthKit cannot atomically replace it. Callers
        // that own the record can fall back to DeleteHealthData + WriteHealthData.
        // Act
        var result = new UpdateHealthDataResult
        {
            Error = UpdateHealthDataError.LegacyRecordNotUpdatable,
        };

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(UpdateHealthDataError.LegacyRecordNotUpdatable, result.Error);
        Assert.Null(result.RecordId);
    }

    [Fact]
    public void WithCrossSourceNotSupported_IsErrorAndSurfacesSpecificValue()
    {
        // Arrange
        // CrossSourceNotSupported is the iOS-specific case where the existing sample was
        // authored by a different app. Sync-identifier replacement is source-scoped, so
        // there is no supported way to update cross-source records.
        // Act
        var result = new UpdateHealthDataResult
        {
            Error = UpdateHealthDataError.CrossSourceNotSupported,
        };

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(UpdateHealthDataError.CrossSourceNotSupported, result.Error);
        Assert.Null(result.RecordId);
    }

    [Fact]
    public void WithErrorException_IsError()
    {
        // Arrange
        var caught = new InvalidOperationException("boom");

        // Act
        var result = new UpdateHealthDataResult
        {
            Error = UpdateHealthDataError.UnexpectedException,
            ErrorException = caught,
        };

        // Assert
        Assert.True(result.IsError);
        Assert.Same(caught, result.ErrorException);
    }

    [Fact]
    public void ErrorExceptionAloneTriggersIsError_EvenWithoutError()
    {
        // Arrange
        // Act
        var result = new UpdateHealthDataResult { ErrorException = new Exception() };

        // Assert
        // Result<TError>.IsSuccess requires BOTH Error == null AND ErrorException == null.
        Assert.True(result.IsError);
    }
}
