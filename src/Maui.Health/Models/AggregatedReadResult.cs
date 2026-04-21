using Maui.Health.Models.Metrics;

namespace Maui.Health.Models;

/// <summary>
/// Result of <see cref="Services.IHealthService.GetAggregatedHealthData{TDto}(HealthTimeRange, System.Threading.CancellationToken)"/> —
/// a single aggregate across the whole requested window (not bucketed; use
/// <see cref="AggregatedIntervalReadResult"/> for the bucketed variant).
/// </summary>
/// <remarks>
/// <para>Success with <see cref="Aggregate"/> non-null means the platform returned a value
/// for the window. Success with <see cref="Aggregate"/> null means the platform confirmed
/// there is no data to aggregate (legitimate empty) or that the requested metric is not
/// supported as an aggregate on this platform.</para>
///
/// <para>Failure (<see cref="Result.IsError"/> true) means the aggregation call itself failed
/// — check <see cref="Result.ErrorException"/> for the reason. Callers should not treat a
/// failed aggregation as "no data."</para>
/// </remarks>
public class AggregatedReadResult : Result
{
    /// <summary>
    /// The single aggregate value for the window, or null if there was nothing to aggregate
    /// or the type is not supported. Always null on failure.
    /// </summary>
    public AggregatedResult? Aggregate { get; init; }
}
