namespace Maui.Health.Constants;

/// <summary>
/// Centralized constants for health-related functionality across all platforms.
/// </summary>
public static class HealthConstants
{
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
        /// Default hear rate limit.
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

    /// <summary>
    /// Unit conversion constants.
    /// </summary>
    public static class UnitConversions
    {
        /// <summary>
        /// Number of grams per kilogram.
        /// </summary>
        public const double GramsPerKilogram = 1000.0;

        /// <summary>
        /// Number of calories per kilocalorie.
        /// </summary>
        public const double CaloriesPerKilocalorie = 1000.0;

        /// <summary>
        /// Number of centimeters per meter.
        /// </summary>
        public const double CentimetersPerMeter = 100.0;

        /// <summary>
        /// Multiplier to convert decimal to percentage (0-100).
        /// </summary>
        public const double PercentageMultiplier = 100.0;
    }

    /// <summary>
    /// Date and time format strings.
    /// </summary>
    public static class DateFormats
    {
        /// <summary>
        /// ISO 8601 UTC date-time format.
        /// </summary>
        public const string Iso8601Utc = "yyyy-MM-ddTHH:mm:ss'Z'";
    }

    /// <summary>
    /// Unit label strings.
    /// </summary>
    public static class Units
    {
        /// <summary>
        /// Kilogram unit label.
        /// </summary>
        public const string Kilogram = "kg";

        /// <summary>
        /// Centimeter unit label.
        /// </summary>
        public const string Centimeter = "cm";

        /// <summary>
        /// Kilocalorie unit label.
        /// </summary>
        public const string Kilocalorie = "kcal";

        /// <summary>
        /// Beats per minute unit label.
        /// </summary>
        public const string BeatsPerMinute = "BPM";

        /// <summary>
        /// Percentage unit label.
        /// </summary>
        public const string Percent = "%";

        /// <summary>
        /// VO2 Max unit label (ml/kg/min).
        /// </summary>
        public const string Vo2Max = "ml/kg/min";

        /// <summary>
        /// Millimeters of mercury unit label.
        /// </summary>
        public const string MillimetersOfMercury = "mmHg";

        /// <summary>
        /// HealthKit VO2 Max unit string.
        /// </summary>
        public const string HKVo2Max = "ml/kg*min";
    }

    /// <summary>
    /// Metadata key constants for workout sessions.
    /// </summary>
    public static class MetadataKeys
    {
        /// <summary>
        /// Active duration in seconds.
        /// </summary>
        public const string ActiveDurationSeconds = "ActiveDurationSeconds";

        /// <summary>
        /// Paused duration in seconds.
        /// </summary>
        public const string PausedDurationSeconds = "PausedDurationSeconds";

        /// <summary>
        /// Number of pauses.
        /// </summary>
        public const string PauseCount = "PauseCount";

        /// <summary>
        /// Pause intervals data.
        /// </summary>
        public const string PauseIntervals = "PauseIntervals";
    }

    /// <summary>
    /// Data origin constants.
    /// </summary>
    public static class DataOrigins
    {
        /// <summary>
        /// iOS HealthKit data origin.
        /// </summary>
        public const string HealthKit = "HealthKit";

        /// <summary>
        /// Unknown data origin.
        /// </summary>
        public const string Unknown = "Unknown";
    }
}
