using Maui.Health.Platforms.Android.Extensions;
using Xunit;

namespace Maui.Health.Tests;

public class BackgroundReadRefusalTests
{
    /// <summary>
    /// Verbatim from a production Health Connect refusal (Android 16, aggregate called after the user left the app).
    /// </summary>
    private const string RefusalMessage =
        "android.health.connect.HealthConnectException: java.lang.SecurityException: com.example.appmust be in foreground to call aggregate method: uid 10527 does not have android.permission.health.READ_HEALTH_DATA_IN_BACKGROUND.";

    [Fact]
    public void IsBackgroundReadRefusal_RefusalRethrownByContinuation_ReturnsTrue()
    {
        // Arrange
        // A suspend call that fails after suspending reaches managed code through Continuation, which
        // rethrows the Throwable as a plain Exception carrying only its message.
        var exception = new Exception(RefusalMessage);

        // Act
        var isBackgroundReadRefusal = exception.IsBackgroundReadRefusal();

        // Assert
        Assert.True(isBackgroundReadRefusal);
    }

    [Fact]
    public void IsBackgroundReadRefusal_RefusalWrappedByReflectiveInvoke_ReturnsTrue()
    {
        // Arrange
        // A suspend call that fails before suspending throws out of Method.Invoke, so the refusal is
        // the cause of a message-less InvocationTargetException.
        var exception = new Exception("Exception_WasThrown, Java.Lang.ReflectiveOperationException", new Exception(RefusalMessage));

        // Act
        var isBackgroundReadRefusal = exception.IsBackgroundReadRefusal();

        // Assert
        Assert.True(isBackgroundReadRefusal);
    }

    [Fact]
    public void IsBackgroundReadRefusal_MissingDataTypePermission_ReturnsFalse()
    {
        // Arrange
        var exception = new Exception("java.lang.SecurityException: Caller doesn't have android.permission.health.READ_STEPS to read record of class StepsRecord");

        // Act
        var isBackgroundReadRefusal = exception.IsBackgroundReadRefusal();

        // Assert
        Assert.False(isBackgroundReadRefusal);
    }
}
