namespace Maui.Health.Enums.Errors;

/// <summary>
/// Errors that can occur when updating an existing health record via
/// <see cref="Services.IHealthService.UpdateHealthData{TDto}(string, TDto, bool, System.Threading.CancellationToken)"/>.
/// </summary>
public enum UpdateHealthDataError
{
    /// <summary>
    /// The current platform does not support updating health data at all.
    /// Returned by the Windows and macOS Catalyst stubs, and by the iOS path when
    /// <c>HKHealthStore.IsHealthDataAvailable</c> is false.
    /// </summary>
    NotSupported = 1,

    /// <summary>
    /// Android only: Health Connect SDK is unavailable or needs an update.
    /// </summary>
    SdkUnavailable = 2,

    /// <summary>
    /// The user did not grant the permission required to update this data type, or the
    /// up-front permission request failed.
    /// </summary>
    PermissionDenied = 3,

    /// <summary>
    /// iOS only: the supplied record ID is not a valid UUID string.
    /// </summary>
    InvalidRecordId = 4,

    /// <summary>
    /// iOS only: no record with the supplied ID could be found in the health store,
    /// so there is nothing to update. The old record is untouched.
    /// </summary>
    RecordNotFound = 5,

    /// <summary>
    /// The supplied DTO could not be converted to the native record type
    /// (Android Health Connect record / iOS <c>HKObject</c>). Neither the old nor any
    /// new record was touched.
    /// </summary>
    DtoConversionFailed = 6,

    /// <summary>
    /// Android: the native <c>updateRecords</c> call failed. The original record
    /// is unchanged.
    /// </summary>
    PlatformUpdateFailed = 7,

    /// <summary>
    /// An unexpected exception was thrown during the update. The exception is
    /// available on <see cref="Maui.Health.Models.Result{TError}.ErrorException"/>.
    /// </summary>
    UnexpectedException = 8,

    /// <summary>
    /// iOS only: the existing record carries no <c>HKMetadataKeySyncIdentifier</c>, so HealthKit
    /// cannot atomically replace it. This happens for samples written by older SDK versions
    /// (before sync-identifier stamping was introduced) or by other apps that don't use this SDK.
    /// Callers that own the record can fall back to
    /// <see cref="Services.IHealthService.DeleteHealthData{TDto}(string, bool, System.Threading.CancellationToken)"/>
    /// followed by <see cref="Services.IHealthService.WriteHealthData{TDto}(TDto, bool, System.Threading.CancellationToken)"/>.
    /// </summary>
    LegacyRecordNotUpdatable = 9,

    /// <summary>
    /// iOS only: the target sample was authored by a different app, so HealthKit will not let this
    /// SDK replace it via the sync-identifier flow (in HealthKit, write/modify operations are
    /// scoped to the calling app's bundle). Both platforms restrict cross-origin modification —
    /// on Android the same attempt surfaces as <see cref="PlatformUpdateFailed"/> instead, because
    /// Health Connect rejects the <c>updateRecords</c> call rather than exposing a dedicated
    /// ownership check up front.
    /// </summary>
    CrossSourceNotSupported = 10,
}
