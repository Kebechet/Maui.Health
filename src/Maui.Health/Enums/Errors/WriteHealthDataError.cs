namespace Maui.Health.Enums.Errors;

/// <summary>
/// Errors that can occur when writing health data via
/// <see cref="Services.IHealthService.WriteHealthData{TDto}(System.Collections.Generic.IList{TDto}, bool, System.Threading.CancellationToken)"/>.
/// </summary>
public enum WriteHealthDataError
{
    /// <summary>
    /// The current platform does not support writing health data at all.
    /// Returned by the Windows and macOS Catalyst stubs, and by the iOS path when
    /// <c>HKHealthStore.IsHealthDataAvailable</c> is false.
    /// </summary>
    NotSupported = 1,

    /// <summary>
    /// Android only: Health Connect SDK is unavailable or needs an update.
    /// Check <see cref="Maui.Health.Models.Result{TError}.ErrorException"/> for the underlying SDK status.
    /// </summary>
    SdkUnavailable = 2,

    /// <summary>
    /// The user did not grant the permission required to write this data type, or the
    /// up-front permission request failed.
    /// </summary>
    PermissionDenied = 3,

    /// <summary>
    /// One of the input DTOs could not be converted to the native record type
    /// (Android Health Connect record / iOS <c>HKObject</c>). The offending input
    /// is implementation-specific; the whole batch is rejected.
    /// </summary>
    DtoConversionFailed = 4,

    /// <summary>
    /// The native store rejected the write (Android <c>insertRecords</c> returned no
    /// response, or iOS <c>HKHealthStore.SaveAll</c> returned false). No records
    /// were persisted.
    /// </summary>
    PlatformWriteFailed = 5,

    /// <summary>
    /// An unexpected exception was thrown during the write. The exception is
    /// available on <see cref="Maui.Health.Models.Result{TError}.ErrorException"/>.
    /// </summary>
    UnexpectedException = 6,
}
