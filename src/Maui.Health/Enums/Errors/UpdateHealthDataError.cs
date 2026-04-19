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
    /// iOS only: the old record was located but the native delete call failed. The
    /// original record is still in the store and has not been modified.
    /// </summary>
    PlatformDeleteFailed = 7,

    /// <summary>
    /// Android: the native <c>updateRecords</c> call failed. The original record
    /// is unchanged.
    /// </summary>
    PlatformUpdateFailed = 8,

    /// <summary>
    /// iOS only: the old record was successfully deleted but the replacement insert
    /// failed. <b>The original data is lost</b> and no new record was written. Callers
    /// should surface this clearly — this is the one failure mode where the pre-update
    /// state cannot be recovered.
    /// </summary>
    PlatformDeleteSucceededButInsertFailed = 9,

    /// <summary>
    /// An unexpected exception was thrown during the update. The exception is
    /// available on <see cref="Maui.Health.Models.Result{TError}.ErrorException"/>.
    /// </summary>
    UnexpectedException = 10,
}
