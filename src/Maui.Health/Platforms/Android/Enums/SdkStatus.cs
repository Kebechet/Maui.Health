namespace Maui.Health.Enums.Errors;

/// <summary>
/// Errors that can occur when checking whether the health SDK is available on the device.
/// https://developer.android.com/reference/androidx/health/connect/client/HealthConnectClient#constants_1
/// </summary>
internal enum SdkStatus
{
    /// <summary>
    /// The Health Connect SDK is unavailable on this device at the time. This can be due to the device running a lower than required Android Version.
    /// https://developer.android.com/reference/androidx/health/connect/client/HealthConnectClient#SDK_UNAVAILABLE()
    /// </summary>
    SdkUnavailable = 1,

    /// <summary>
    /// The Health Connect SDK APIs are currently unavailable, the provider is either not installed or needs to be updated.
    /// Apps may choose to redirect to package installers to find a suitable APK.
    /// https://developer.android.com/reference/androidx/health/connect/client/HealthConnectClient#SDK_UNAVAILABLE_PROVIDER_UPDATE_REQUIRED()
    /// </summary>
    SdkUnavailableProviderUpdateRequired = 2,

    /// <summary>
    /// The Health Connect SDK APIs are available.
    /// Apps can subsequently call getOrCreate to get an instance of HealthConnectClient.
    /// https://developer.android.com/reference/androidx/health/connect/client/HealthConnectClient#SDK_AVAILABLE()
    /// </summary>
    SdkAvailable = 3
}
