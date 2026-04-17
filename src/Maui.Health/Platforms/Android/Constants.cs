namespace Maui.Health.Platforms.Android;

/// <summary>
/// Android-specific constants for Health Connect integration.
/// </summary>
public static class AndroidConstant
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
    /// Prefix for all Health Connect permission strings.
    /// Used to detect whether any health permission is currently granted.
    /// </summary>
    public const string HealthPermissionPrefix = "android.permission.health.";

    /// <summary>
    /// Preferences key for persisting the UTC Unix-millisecond timestamp of when the user
    /// first granted any Health Connect permission. Health Connect allows reads up to
    /// <see cref="HealthConnectDefaultHistoryDays"/> days prior to this timestamp without
    /// <see cref="FullHistoryReadPermission"/>, and the platform exposes no API to read this
    /// value back — we must persist it ourselves.
    /// </summary>
    public const string FirstPermissionGrantAtKey = "maui_health_first_permission_grant_at_unix_ms";

    /// <summary>
    /// Default historical read window Health Connect allows without
    /// <see cref="FullHistoryReadPermission"/>. Measured in days prior to "when any permission
    /// was first granted".
    /// https://developer.android.com/health-and-fitness/guides/health-connect/develop/read-data#read-data-older-than-30-days
    /// </summary>
    public const int HealthConnectDefaultHistoryDays = 30;

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

        /// <summary>
        /// Percentage class full name for reflection.
        /// </summary>
        public const string PercentageClassName = "androidx.health.connect.client.units.Percentage";

        /// <summary>
        /// StepsRecord class full name for aggregate metric reflection.
        /// </summary>
        public const string StepsRecordClassName = "androidx.health.connect.client.records.StepsRecord";

        /// <summary>
        /// ActiveCaloriesBurnedRecord class full name for aggregate metric reflection.
        /// </summary>
        public const string ActiveCaloriesBurnedRecordClassName = "androidx.health.connect.client.records.ActiveCaloriesBurnedRecord";

        /// <summary>
        /// HydrationRecord class full name for aggregate metric reflection.
        /// </summary>
        public const string HydrationRecordClassName = "androidx.health.connect.client.records.HydrationRecord";

        /// <summary>
        /// WeightRecord class full name for aggregate metric reflection.
        /// </summary>
        public const string WeightRecordClassName = "androidx.health.connect.client.records.WeightRecord";

        /// <summary>
        /// HeartRateRecord class full name for aggregate metric reflection.
        /// </summary>
        public const string HeartRateRecordClassName = "androidx.health.connect.client.records.HeartRateRecord";

        /// <summary>
        /// COUNT_TOTAL aggregate metric field name on StepsRecord.
        /// </summary>
        public const string CountTotalMetricName = "COUNT_TOTAL";

        /// <summary>
        /// ACTIVE_CALORIES_TOTAL aggregate metric field name on ActiveCaloriesBurnedRecord.
        /// </summary>
        public const string ActiveCaloriesTotalMetricName = "ACTIVE_CALORIES_TOTAL";

        /// <summary>
        /// VOLUME_TOTAL aggregate metric field name on HydrationRecord.
        /// </summary>
        public const string VolumeTotalMetricName = "VOLUME_TOTAL";

        /// <summary>
        /// WEIGHT_AVG aggregate metric field name on WeightRecord.
        /// </summary>
        public const string WeightAvgMetricName = "WEIGHT_AVG";

        /// <summary>
        /// BPM_AVG aggregate metric field name on HeartRateRecord.
        /// </summary>
        public const string BpmAvgMetricName = "BPM_AVG";

        /// <summary>
        /// AggregateMetric class full name for reflection.
        /// </summary>
        public const string AggregateMetricClassName = "androidx.health.connect.client.aggregate.AggregateMetric";

        /// <summary>
        /// AggregateRequest class full name for reflection.
        /// </summary>
        public const string AggregateRequestClassName = "androidx.health.connect.client.request.AggregateRequest";

        /// <summary>
        /// AggregateGroupByDurationRequest class full name for reflection.
        /// </summary>
        public const string AggregateGroupByDurationRequestClassName = "androidx.health.connect.client.request.AggregateGroupByDurationRequest";

        /// <summary>
        /// ChangesTokenRequest class full name for reflection.
        /// </summary>
        public const string ChangesTokenRequestClassName = "androidx.health.connect.client.request.ChangesTokenRequest";
    }
}
