using AndroidX.Health.Connect.Client;

namespace Maui.Health.Platforms.Android.Extensions;

/// <summary>
/// Extension methods for <see cref="IHealthConnectClient"/>.
/// Wraps Health Connect features.getFeatureStatus() API for checking device capabilities.
/// Features tied to the system module remain unavailable on Android 13 and lower.
/// </summary>
internal static class IHealthConnectClientExtensions
{
    /// <summary>
    /// Checks if reading health data in the background is supported on this device.
    /// Requires android.permission.health.READ_HEALTH_DATA_IN_BACKGROUND in manifest.
    /// </summary>
    internal static bool IsBackgroundReadSupported(this IHealthConnectClient healthConnectClient)
    {
        return healthConnectClient.IsFeatureAvailable(HealthConnectFeatures.FeatureReadHealthDataInBackground);
    }

    /// <summary>
    /// Checks if reading full health data history (beyond 30 days) is supported on this device.
    /// </summary>
    internal static bool IsHealthDataHistorySupported(this IHealthConnectClient healthConnectClient)
    {
        return healthConnectClient.IsFeatureAvailable(HealthConnectFeatures.FeatureReadHealthDataHistory);
    }

    /// <summary>
    /// Checks if planned exercise features are supported on this device.
    /// </summary>
    internal static bool IsPlannedExerciseSupported(this IHealthConnectClient healthConnectClient)
    {
        return healthConnectClient.IsFeatureAvailable(HealthConnectFeatures.FeaturePlannedExercise);
    }

    /// <summary>
    /// Checks if skin temperature recording is supported on this device.
    /// </summary>
    internal static bool IsSkinTemperatureSupported(this IHealthConnectClient healthConnectClient)
    {
        return healthConnectClient.IsFeatureAvailable(HealthConnectFeatures.FeatureSkinTemperature);
    }

    /// <summary>
    /// Checks if a specific Health Connect feature is available on this device.
    /// </summary>
    /// <param name="healthConnectClient">The Health Connect client instance</param>
    /// <param name="feature">The feature constant from <see cref="HealthConnectFeatures"/></param>
    /// <returns>True if the feature is available, false otherwise</returns>
    internal static bool IsFeatureAvailable(this IHealthConnectClient healthConnectClient, int feature)
    {
        try
        {
            var status = healthConnectClient.Features.GetFeatureStatus(feature);
            return status == HealthConnectFeatures.FeatureStatusAvailable;
        }
        catch
        {
            return false;
        }
    }
}
