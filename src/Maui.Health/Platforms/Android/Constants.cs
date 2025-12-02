namespace Maui.Health.Platforms.Android;

/// <summary>
/// Android-specific constants for Health Connect integration.
/// </summary>
public static class AndroidConstants
{
    /// <summary>
    /// Minimum Android API version required for Health Connect SDK.
    /// </summary>
    public const int MinimumApiVersionRequired = 26; // Android 8.0

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

    /// <summary>
    /// Maximum number of records per Health Connect request.
    /// This is an Android Health Connect API limitation - the SDK enforces a maximum of 1000 records per read request.
    /// See: https://developer.android.com/health-and-fitness/health-connect/read-data
    /// </summary>
    public const int MaxRecordsPerRequest = 1000;

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
