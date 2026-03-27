namespace Maui.Health.Constants;

/// <summary>
/// Data origin constants.
/// </summary>
public static class DataOrigin
{
    /// <summary>
    /// iOS HealthKit data origin.
    /// </summary>
    public const string HealthKitOrigin = "HealthKit";

    /// <summary>
    /// Android Health Connect data origin.
    /// </summary>
    public const string HealthConnectOrigin = "HealthConnect";

    /// <summary>
    /// Unknown data origin.
    /// </summary>
    public const string Unknown = nameof(Unknown);
}
