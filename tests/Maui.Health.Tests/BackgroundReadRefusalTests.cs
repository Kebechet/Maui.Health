using Maui.Health.Platforms.Android.Extensions;
using Xunit;

namespace Maui.Health.Tests;

public class BackgroundReadRefusalTests
{
    /// <summary>
    /// A production Health Connect refusal (Android 16, aggregate called after the user left the app), with the
    /// package name and uid replaced.
    /// </summary>
    private const string RefusalMessage =
        "android.health.connect.HealthConnectException: java.lang.SecurityException: com.example.appmust be in foreground to call aggregate method: uid 10527 does not have android.permission.health.READ_HEALTH_DATA_IN_BACKGROUND.";

    /// <summary>
    /// The aggregate refusal of the Android 14 initial release, which names no permission.
    /// </summary>
    private const string InitialReleaseAggregateRefusalMessage =
        "android.health.connect.HealthConnectException: java.lang.SecurityException: com.example.appmust be in foreground to call aggregate method";

    /// <summary>
    /// The change-log refusal of the Android 14 initial release, which names no permission.
    /// </summary>
    private const string InitialReleaseChangeLogsRefusalMessage =
        "android.health.connect.HealthConnectException: java.lang.SecurityException: com.example.app must be in foreground to read the change logs";

    /// <summary>
    /// Raised when a filtered read is limited to the app's own records: in the background without the permission,
    /// or in the foreground with write-only access.
    /// </summary>
    private const string OtherAppsReadRefusalMessage =
        "android.health.connect.HealthConnectException: java.lang.SecurityException: Caller does not have permission to read data for the following (recordType: class android.health.connect.datatypes.StepsRecord) from other applications.";

    /// <summary>
    /// Raised by Jetpack's readRecord when no record comes back for the ID, whether it is missing or belongs to
    /// another app while reads are limited to the app's own records.
    /// </summary>
    private const string NoRecordsMessage = "No records";

    private const string MissingDataTypePermissionMessage =
        "java.lang.SecurityException: Caller doesn't have android.permission.health.READ_STEPS to read record of class StepsRecord";

    [Fact]
    public void IsBackgroundReadRefusal_RefusalRethrownByContinuation_ReturnsTrue()
    {
        // Arrange
        // A suspend call that fails after suspending reaches managed code through Continuation, which
        // rethrows the Throwable as a plain Exception carrying only its message.
        var exception = new Exception(RefusalMessage);

        // Act
        var isBackgroundReadRefusal = exception.IsBackgroundReadRefusal(() => false);

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
        var isBackgroundReadRefusal = exception.IsBackgroundReadRefusal(() => false);

        // Assert
        Assert.True(isBackgroundReadRefusal);
    }

    [Theory]
    [InlineData(InitialReleaseAggregateRefusalMessage)]
    [InlineData(InitialReleaseChangeLogsRefusalMessage)]
    public void IsBackgroundReadRefusal_InitialReleaseRefusalWithoutPermissionName_ReturnsTrue(string message)
    {
        // Arrange
        var exception = new Exception(message);

        // Act
        var isBackgroundReadRefusal = exception.IsBackgroundReadRefusal(() => false);

        // Assert
        Assert.True(isBackgroundReadRefusal);
    }

    [Fact]
    public void IsBackgroundReadRefusal_RefusalNamingThePermission_DoesNotCheckAppState()
    {
        // Arrange
        var exception = new Exception(RefusalMessage);
        var appStateCheckCount = 0;

        // Act
        exception.IsBackgroundReadRefusal(() =>
        {
            appStateCheckCount++;
            return false;
        });

        // Assert
        Assert.Equal(0, appStateCheckCount);
    }

    [Theory]
    [InlineData(OtherAppsReadRefusalMessage)]
    [InlineData(NoRecordsMessage)]
    public void IsBackgroundReadRefusal_SelfReadRestrictionInBackgroundWithoutPermission_ReturnsTrue(string message)
    {
        // Arrange
        var exception = new Exception(message);

        // Act
        var isBackgroundReadRefusal = exception.IsBackgroundReadRefusal(() => true);

        // Assert
        Assert.True(isBackgroundReadRefusal);
    }

    [Theory]
    [InlineData(OtherAppsReadRefusalMessage)]
    [InlineData(NoRecordsMessage)]
    public void IsBackgroundReadRefusal_SelfReadRestrictionInForeground_ReturnsFalse(string message)
    {
        // Arrange
        // In the foreground the same messages mean write-only access or a record that is not there.
        var exception = new Exception(message);

        // Act
        var isBackgroundReadRefusal = exception.IsBackgroundReadRefusal(() => false);

        // Assert
        Assert.False(isBackgroundReadRefusal);
    }

    [Fact]
    public void IsBackgroundReadRefusal_MissingDataTypePermissionInBackground_ReturnsFalse()
    {
        // Arrange
        var exception = new Exception(MissingDataTypePermissionMessage);

        // Act
        var isBackgroundReadRefusal = exception.IsBackgroundReadRefusal(() => true);

        // Assert
        Assert.False(isBackgroundReadRefusal);
    }
}
