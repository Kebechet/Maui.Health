using Maui.Health.Enums.Errors;
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
/// <see cref="Result.ErrorException"/>. <see cref="Workouts"/> is empty. A read refused because
/// the app is in the background sets <see cref="Result{TError}.Error"/> to
/// <see cref="ReadHealthDataError.BackgroundReadNotPermitted"/>.</para>
/// </remarks>
public class WorkoutReadResult : Result<ReadHealthDataError>
{
    /// <summary>
    /// The workout records returned by the platform. Empty on failure; may also be
    /// legitimately empty on success — check <see cref="Result.IsSuccess"/> to distinguish.
    /// </summary>
    public IReadOnlyList<WorkoutDto> Workouts { get; init; } = [];
}
