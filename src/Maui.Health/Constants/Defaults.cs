namespace Maui.Health.Constants;

/// <summary>
/// Default values for health operations.
/// </summary>
public static class Defaults
{
    /// <summary>
    /// Default time threshold in minutes for detecting duplicate workouts.
    /// </summary>
    public const int DuplicateThresholdMinutes = 5;

    /// <summary>
    /// Default fallback weight in kilograms.
    /// </summary>
    public const double FallbackWeightKg = 70.0;

    /// <summary>
    /// Default fallback height in centimeters.
    /// </summary>
    public const double FallbackHeightCm = 175.0;

    /// <summary>
    /// Default fallback value for metrics.
    /// </summary>
    public const double FallbackValue = 0.0;

    /// <summary>
    /// Default heart rate limit.
    /// </summary>
    public const int HeartRateLimit = 0;

    /// <summary>
    /// Single record limit for queries.
    /// </summary>
    public const int SingleRecordLimit = 1;

    /// <summary>
    /// Default timestamp value.
    /// </summary>
    public const long DefaultTimestampValue = 0L;
}
