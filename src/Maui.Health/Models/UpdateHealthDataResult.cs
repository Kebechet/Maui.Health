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
/// <item><description><b>iOS</b> (HealthKit): a <b>new</b> UUID. The iOS update path writes a replacement
/// sample carrying the existing record's <c>HKMetadataKeySyncIdentifier</c> with a bumped
/// <c>HKMetadataKeySyncVersion</c>, which HealthKit swaps in atomically. The replacement sample is
/// assigned a fresh UUID, so callers must re-link their local record to it.</description></item>
/// </list>
/// <para>On failure, <see cref="RecordId"/> is <c>null</c> and all failure modes leave the original
/// record untouched. The typed <see cref="Result{TError}.Error"/> identifies the cause — iOS-only
/// constraints include <see cref="UpdateHealthDataError.LegacyRecordNotUpdatable"/> (existing sample
/// has no sync identifier — e.g. written by an older SDK or another app) and
/// <see cref="UpdateHealthDataError.CrossSourceNotSupported"/> (sample authored by a different app;
/// HealthKit scopes sync-identifier replacement per source bundle).</para>
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
