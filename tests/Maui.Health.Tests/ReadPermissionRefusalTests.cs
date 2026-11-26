using Maui.Health.Platforms.Android.Extensions;
using Xunit;

namespace Maui.Health.Tests;

public class ReadPermissionRefusalTests
{
    /// <summary>
    /// Health Connect on a Samsung A53 (Android 16), an aggregate read of steps made with shouldCheckPermissions: false
    /// while READ_STEPS was refused.
    /// </summary>
    private const string AggregateRefusalMessage =
        "android.health.connect.HealthConnectException: java.lang.SecurityException: Caller doesn't have android.permission.health.READ_STEPS to read to record typeclass android.health.connect.datatypes.StepsRecord";

    /// <summary>
    /// Health Connect on a Pixel 6 (Android 16), a read of steps made with shouldCheckPermissions: false while
    /// READ_STEPS was not granted. The wording differs from the Galaxy A53's.
    /// </summary>
    private const string PixelReadRefusalMessage =
        "android.health.connect.HealthConnectException: java.lang.SecurityException: Caller requires android.permission.health.READ_STEPS to read record type class android.health.connect.datatypes.StepsRecord";

    private const string RecordReadRefusalMessage =
        "java.lang.SecurityException: Caller doesn't have android.permission.health.READ_STEPS to read record of class StepsRecord";

    private const string BackgroundRefusalMessage =
        "android.health.connect.HealthConnectException: java.lang.SecurityException: com.example.appmust be in foreground to call aggregate method: uid 10527 does not have android.permission.health.READ_HEALTH_DATA_IN_BACKGROUND.";

    [Theory]
    [InlineData(AggregateRefusalMessage)]
    [InlineData(RecordReadRefusalMessage)]
    [InlineData(PixelReadRefusalMessage)]
    public void IsReadPermissionRefusal_MissingReadPermission_ReturnsTrue(string message)
    {
        // Arrange
        var exception = new Exception(message);

        // Act
        var isReadPermissionRefusal = exception.IsReadPermissionRefusal();

        // Assert
        Assert.True(isReadPermissionRefusal);
    }

    [Fact]
    public void IsReadPermissionRefusal_RefusalWrappedByReflectiveInvoke_ReturnsTrue()
    {
        // Arrange
        var exception = new Exception("Exception_WasThrown, Java.Lang.ReflectiveOperationException", new Exception(AggregateRefusalMessage));

        // Act
        var isReadPermissionRefusal = exception.IsReadPermissionRefusal();

        // Assert
        Assert.True(isReadPermissionRefusal);
    }

    [Theory]
    [InlineData(BackgroundRefusalMessage)]
    [InlineData("java.io.IOException: Binder died")]
    public void IsReadPermissionRefusal_OtherFailure_ReturnsFalse(string message)
    {
        // Arrange
        var exception = new Exception(message);

        // Act
        var isReadPermissionRefusal = exception.IsReadPermissionRefusal();

        // Assert
        Assert.False(isReadPermissionRefusal);
    }
}
