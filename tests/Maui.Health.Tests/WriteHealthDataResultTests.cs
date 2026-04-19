using Maui.Health.Enums.Errors;
using Maui.Health.Models;
using Xunit;

namespace Maui.Health.Tests;

public class WriteHealthDataResultTests
{
    [Fact]
    public void Default_HasEmptyRecordIdsAndNoError_AndReportsSuccess()
    {
        // Arrange
        // Act
        var result = new WriteHealthDataResult();

        // Assert
        // A parameterless result with no Error/ErrorException is considered a success by
        // Result<TError>. Kept as-is to match the idiom; producers should always set Error
        // on failure branches (never rely on "empty result = success" implicitly).
        Assert.True(result.IsSuccess);
        Assert.False(result.IsError);
        Assert.Empty(result.RecordIds);
        Assert.Null(result.Error);
        Assert.Null(result.ErrorException);
    }

    [Fact]
    public void WithRecordIds_IsSuccessAndExposesIds()
    {
        // Arrange
        var ids = new[] { "id-1", "id-2", "id-3" };

        // Act
        var result = new WriteHealthDataResult { RecordIds = ids };

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ids, result.RecordIds);
    }

    [Fact]
    public void WithError_IsErrorAndRecordIdsEmpty()
    {
        // Arrange
        // Act
        var result = new WriteHealthDataResult { Error = WriteHealthDataError.PermissionDenied };

        // Assert
        Assert.True(result.IsError);
        Assert.False(result.IsSuccess);
        Assert.Equal(WriteHealthDataError.PermissionDenied, result.Error);
        Assert.Empty(result.RecordIds);
    }

    [Fact]
    public void WithErrorException_IsError()
    {
        // Arrange
        var caught = new InvalidOperationException("boom");

        // Act
        var result = new WriteHealthDataResult
        {
            Error = WriteHealthDataError.UnexpectedException,
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
        var result = new WriteHealthDataResult { ErrorException = new Exception() };

        // Assert
        // Result<TError>.IsSuccess requires BOTH Error == null AND ErrorException == null.
        // This guards against producers that forget to set the typed Error when rethrowing.
        Assert.True(result.IsError);
    }
}
