using Maui.Health.Models.Metrics;

namespace Maui.Health.Models;

/// <summary>
/// Result of <see cref="Services.IHealthService.GetHealthRecord{TDto}(string, bool, System.Threading.CancellationToken)"/>.
/// </summary>
/// <remarks>
/// <para>Success with <see cref="Record"/> non-null means the record exists on the device.
/// Success with <see cref="Record"/> null means the platform confirmed the record does not
/// exist (e.g., wrong ID, deleted by another app).</para>
///
/// <para>Failure (<see cref="Result.IsError"/> true) means the platform call itself failed:
/// permission denial, SDK unavailable, query error. In failure cases <see cref="Record"/> is
/// always null, but the converse is not true — check <see cref="Result.IsSuccess"/> to
/// distinguish "not found" from "platform failed."</para>
/// </remarks>
/// <typeparam name="TDto">The type of health metric DTO requested.</typeparam>
public class HealthRecordReadResult<TDto> : Result
    where TDto : HealthMetricBase
{
    /// <summary>
    /// The single matched record, or null. On failure always null; on success null means the
    /// platform confirmed no such record exists.
    /// </summary>
    public TDto? Record { get; init; }
}
