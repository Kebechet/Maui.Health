using Maui.Health.Models.Metrics;

namespace Maui.Health.Models;

/// <summary>
/// Result of <see cref="Services.IHealthService.GetAggregatedHealthDataByInterval{TDto}(HealthTimeRange, System.TimeSpan, System.Threading.CancellationToken)"/>.
/// </summary>
/// <remarks>
/// <para>Success: <see cref="Result.IsSuccess"/> is <c>true</c> and <see cref="Buckets"/> holds
/// the aggregated rows the platform returned for the requested window. A successful call can
/// still produce an empty <see cref="Buckets"/> list — that means the platform confirmed there
/// is no recorded data in the window, not that something went wrong.</para>
///
/// <para>Failure: <see cref="Result.IsError"/> is <c>true</c> and
/// <see cref="Result.ErrorException"/> carries the platform exception. Common causes on
/// Android Health Connect include <c>IllegalArgumentException</c> when the requested range
/// exceeds the platform's 5000-bucket limit, permission denial, and SDK unavailability; on
/// iOS HealthKit, authorization and query-level failures. In all failure cases
/// <see cref="Buckets"/> is empty.</para>
///
/// <para>This distinction matters for sync orchestration: callers should not treat a failed
/// read as "no data" and advance their sync watermark — the underlying data may still be
/// there and retrievable on a subsequent, narrower call.</para>
/// </remarks>
public class AggregatedIntervalReadResult : Result
{
    /// <summary>
    /// The aggregated buckets returned by the platform. Empty on failure; may also be
    /// legitimately empty on success — check <see cref="Result.IsSuccess"/> to distinguish.
    /// </summary>
    public IReadOnlyList<AggregatedResult> Buckets { get; init; } = [];
}
