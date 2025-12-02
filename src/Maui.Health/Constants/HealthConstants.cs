namespace Maui.Health.Constants;

/// <summary>
/// Centralized constants for health-related functionality across all platforms.
/// </summary>
public static class HealthConstants
{
    /// <summary>
    /// Android-specific constants.
    /// </summary>
    public static class Android
    {
        /// <summary>
        /// Minimum Android API version required for Health Connect SDK.
        /// </summary>
        public const int MinimumApiVersion = 26;

        /// <summary>
        /// Health Connect provider package name.
        /// </summary>
        public const string HealthConnectPackage = "com.google.android.apps.healthdata";

        /// <summary>
        /// Google Play Store package name.
        /// </summary>
        public const string PlayStorePackage = "com.android.vending";

        /// <summary>
        /// Permission for reading full health data history.
        /// </summary>
        public const string FullHistoryReadPermission = "android.permission.health.READ_HEALTH_DATA_HISTORY";

        /// <summary>
        /// Play Store URI template for Health Connect installation.
        /// </summary>
        public const string PlayStoreUriTemplate = "market://details?id={0}&url=healthconnect%3A%2F%2Fonboarding";

        /// <summary>
        /// Intent extra key for overlay.
        /// </summary>
        public const string IntentExtraOverlay = "overlay";

        /// <summary>
        /// Intent extra key for caller ID.
        /// </summary>
        public const string IntentExtraCaller = "callerId";

        /// <summary>
        /// JNI handle field name for reflection.
        /// </summary>
        public const string JniHandleFieldName = "handle";

        /// <summary>
        /// Kotlin companion object field name.
        /// </summary>
        public const string KotlinCompanionFieldName = "Companion";

        /// <summary>
        /// AndroidX Health Connect Units namespace.
        /// </summary>
        public const string HealthConnectUnitsNamespace = "AndroidX.Health.Connect.Client.Units";
    }

    /// <summary>
    /// Default values for health operations.
    /// </summary>
    public static class Defaults
    {
        /// <summary>
        /// Maximum number of records per Health Connect request.
        /// This is an Android Health Connect API limitation - the SDK enforces a maximum of 1000 records per read request.
        /// iOS HealthKit does not have this limitation.
        /// See: https://developer.android.com/health-and-fitness/health-connect/read-data
        /// </summary>
        public const int MaxRecordsPerRequest = 1000;

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

    /// <summary>
    /// Reflection-related constants for Android interop.
    /// </summary>
    public static class Reflection
    {
        /// <summary>
        /// Regex pattern for extracting numbers from strings.
        /// </summary>
        public const string NumberExtractionPattern = @"(\d+\.?\d*)";

        /// <summary>
        /// Mass class full name for reflection.
        /// </summary>
        public const string MassClassName = "androidx.health.connect.client.units.Mass";

        /// <summary>
        /// Length class full name for reflection.
        /// </summary>
        public const string LengthClassName = "androidx.health.connect.client.units.Length";

        /// <summary>
        /// Energy class full name for reflection.
        /// </summary>
        public const string EnergyClassName = "androidx.health.connect.client.units.Energy";

        /// <summary>
        /// Kilograms factory method name.
        /// </summary>
        public const string KilogramsMethodName = "kilograms";

        /// <summary>
        /// Meters factory method name.
        /// </summary>
        public const string MetersMethodName = "meters";

        /// <summary>
        /// Kilocalories factory method name.
        /// </summary>
        public const string KilocaloriesMethodName = "kilocalories";
    }
}
