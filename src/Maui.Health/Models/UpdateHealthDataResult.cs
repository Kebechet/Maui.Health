using Maui.Health.Enums.Errors;

namespace Maui.Health.Models;

/// <summary>
/// Result of <see cref="Services.IHealthService.UpdateHealthData{TDto}(string, TDto, bool, System.Threading.CancellationToken)"/>.
/// Inherits <see cref="Result{TError}"/>, so callers can check success with
/// <see cref="Result{TError}.IsSuccess"/> or the typed <see cref="Result{TError}.Error"/>,
/// and surface any native-layer exception via <see cref="Result{TError}.ErrorException"/>.
/// </summary>
/// <remarks>
/// <para>On success, <see cref="RecordId"/> holds the native ID of the updated record:</para>
/// <list type="bullet">
/// <item><description><b>Android</b> (Health Connect): the same ID the caller passed in. Health Connect's
/// <c>updateRecords</c> is a true in-place update; the record ID is preserved.</description></item>
/// <item><description><b>iOS</b> (HealthKit): a <b>new</b> UUID. HealthKit records are immutable, so
/// update is emulated as delete-by-old-UUID followed by insert. Callers must re-link their local record
/// to the new ID — the old ID is no longer valid.</description></item>
/// </list>
/// <para>On failure, <see cref="RecordId"/> is <c>null</c>. The typed <see cref="Result{TError}.Error"/>
/// identifies whether the pre-update state is recoverable:</para>
/// <list type="bullet">
/// <item><description>Most failure modes (<see cref="UpdateHealthDataError.SdkUnavailable"/>,
/// <see cref="UpdateHealthDataError.PermissionDenied"/>, <see cref="UpdateHealthDataError.RecordNotFound"/>,
/// <see cref="UpdateHealthDataError.DtoConversionFailed"/>, <see cref="UpdateHealthDataError.PlatformDeleteFailed"/>,
/// <see cref="UpdateHealthDataError.PlatformUpdateFailed"/>) leave the original record untouched.</description></item>
/// <item><description><see cref="UpdateHealthDataError.PlatformDeleteSucceededButInsertFailed"/> is the
/// exception: on iOS the old record has been deleted but the replacement insert failed, so the data
/// is lost. Callers should surface this clearly.</description></item>
/// </list>
/// </remarks>
public class UpdateHealthDataResult : Result<UpdateHealthDataError>
{
    /// <summary>
    /// Native ID of the updated record on success; <c>null</c> on failure.
    /// On Android this is the same as the caller-supplied ID; on iOS it is a new UUID
    /// (see the remarks on <see cref="UpdateHealthDataResult"/>).
    /// </summary>
    public string? RecordId { get; init; }
}
