using Maui.Health.Models.Metrics;

namespace Maui.Health.Models;

/// <summary>
/// Result of <see cref="Services.IHealthWorkoutService.Read(HealthTimeRange)"/>.
/// </summary>
/// <remarks>
/// <para>Success: <see cref="Workouts"/> holds the workout rows the platform returned for
/// the requested window; an empty list on success means the platform confirmed no workouts
/// exist in the window.</para>
///
/// <para>Failure (<see cref="Result.IsError"/> true): the platform call failed; check
/// <see cref="Result.ErrorException"/>. <see cref="Workouts"/> is empty.</para>
/// </remarks>
public class WorkoutReadResult : Result
{
    /// <summary>
    /// The workout records returned by the platform. Empty on failure; may also be
    /// legitimately empty on success — check <see cref="Result.IsSuccess"/> to distinguish.
    /// </summary>
    public IReadOnlyList<WorkoutDto> Workouts { get; init; } = [];
}
