using Maui.Health.Models.Metrics;

namespace Maui.Health.Models;

/// <summary>
/// Result of <see cref="Services.IHealthService.GetHealthData{TDto}(HealthTimeRange, bool, System.Threading.CancellationToken)"/>.
/// </summary>
/// <remarks>
/// <para>Success: <see cref="Result.IsSuccess"/> is <c>true</c> and <see cref="Records"/> holds
/// the rows the platform returned for the requested window. A successful call can still
/// produce an empty <see cref="Records"/> list — that means the platform confirmed there is
/// no recorded data in the window, not that something went wrong.</para>
///
/// <para>Failure: <see cref="Result.IsError"/> is <c>true</c> and
/// <see cref="Result.ErrorException"/> carries the platform exception (permission denial,
/// SDK unavailable, platform query error, cancellation). In all failure cases
/// <see cref="Records"/> is empty.</para>
///
/// <para>This distinction matters for sync orchestration: callers should not treat a failed
/// read as "no data" and advance a sync watermark — the data may still be there and
/// retrievable on a subsequent call.</para>
/// </remarks>
/// <typeparam name="TDto">The type of health metric DTO requested.</typeparam>
public class HealthDataReadResult<TDto> : Result
    where TDto : HealthMetricBase
{
    /// <summary>
    /// The records returned by the platform. Empty on failure; may also be legitimately empty
    /// on success — check <see cref="Result.IsSuccess"/> to distinguish.
    /// </summary>
    public IReadOnlyList<TDto> Records { get; init; } = [];
}
