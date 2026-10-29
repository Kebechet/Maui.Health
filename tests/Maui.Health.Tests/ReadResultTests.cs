using Maui.Health.Enums.Errors;
using Maui.Health.Models;
using Maui.Health.Models.Metrics;
using Xunit;

namespace Maui.Health.Tests;

public class ReadResultTests
{
    [Fact]
    public void HealthDataReadResult_BackgroundReadNotPermitted_IsErrorWithoutRecords()
    {
        // Arrange
        // Act
        var result = new HealthDataReadResult<StepsDto> { Error = ReadHealthDataError.BackgroundReadNotPermitted };

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ReadHealthDataError.BackgroundReadNotPermitted, result.Error);
        Assert.Null(result.ErrorException);
        Assert.Empty(result.Records);
    }

    [Fact]
    public void AggregatedIntervalReadResult_BackgroundReadNotPermitted_IsErrorWithoutBuckets()
    {
        // Arrange
        // Act
        var result = new AggregatedIntervalReadResult { Error = ReadHealthDataError.BackgroundReadNotPermitted };

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ReadHealthDataError.BackgroundReadNotPermitted, result.Error);
        Assert.Empty(result.Buckets);
    }

    [Fact]
    public void AggregatedIntervalReadResult_ExceptionOnly_IsErrorWithoutTypedError()
    {
        // Arrange
        // Act
        var result = new AggregatedIntervalReadResult { ErrorException = new InvalidOperationException("boom") };

        // Assert
        Assert.True(result.IsError);
        Assert.Null(result.Error);
    }
}
