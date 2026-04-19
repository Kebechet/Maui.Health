using Maui.Health.Enums.Errors;

namespace Maui.Health.Models;

/// <summary>
/// Result of <see cref="Services.IHealthService.WriteHealthData{TDto}(System.Collections.Generic.IList{TDto}, bool, System.Threading.CancellationToken)"/>.
/// Inherits <see cref="Result{TError}"/>, so callers can check success with <see cref="Result{TError}.IsSuccess"/>
/// or the typed <see cref="Result{TError}.Error"/>, and surface any native-layer exception via
/// <see cref="Result{TError}.ErrorException"/>.
/// </summary>
/// <remarks>
/// <para>On success, <see cref="RecordIds"/> is 1:1 with the input list — <c>RecordIds[i]</c> is
/// the platform-assigned ID for <c>items[i]</c>. On failure, <see cref="RecordIds"/> is empty.</para>
/// <para>Per-platform ID source:</para>
/// <list type="bullet">
/// <item><description><b>Android</b> (Health Connect): <c>InsertRecordsResponse.RecordIdsList</c></description></item>
/// <item><description><b>iOS</b> (HealthKit): <c>HKObject.Uuid.ToString()</c></description></item>
/// </list>
/// </remarks>
public class WriteHealthDataResult : Result<WriteHealthDataError>
{
    /// <summary>
    /// Platform-assigned record IDs, 1:1 with the input list on success; empty on failure.
    /// </summary>
    public IReadOnlyList<string> RecordIds { get; init; } = [];
}
