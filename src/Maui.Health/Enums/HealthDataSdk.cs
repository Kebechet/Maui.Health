namespace Maui.Health.Enums;

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
}
