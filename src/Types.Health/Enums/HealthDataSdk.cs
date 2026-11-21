namespace Kebechet.Types.Health.Enums;

/// <summary>
/// The health data provider used on the current device.
/// </summary>
public enum HealthDataSdk
{
    /// <summary>Platform not recognized or not applicable</summary>
    Unknown = 0,

    /// <summary>Google Health Connect (Android)</summary>
    GoogleHealthConnect = 1,

    /// <summary>Apple HealthKit (iOS)</summary>
    AppleHealthKit = 2,

    /// <summary>
    /// Google Health Services (Wear OS), which records workouts and their live data on a watch.
    /// https://developer.android.com/health-and-fitness/guides/health-services
    /// </summary>
    GoogleHealthServices = 3,
}
