using Maui.Health.Enums;

namespace Maui.Health.Models.Metrics;

/// <summary>
/// Represents a workout/exercise session from health platforms
/// </summary>
public class WorkoutDto : HealthMetricBase, IHealthTimeRange
{
    /// <summary>
    /// Type of activity performed
    /// </summary>
    public required ActivityType ActivityType { get; init; }

    /// <summary>
    /// Name or title of the workout (if available)
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Start time of the workout session
    /// </summary>
    public required DateTimeOffset StartTime { get; init; }

    /// <summary>
    /// End time of the workout session
    /// </summary>
    public required DateTimeOffset EndTime { get; init; }

    /// <summary>
    /// Duration of the workout in seconds
    /// </summary>
    public double DurationSeconds => (EndTime - StartTime).TotalSeconds;

    /// <summary>
    /// Total energy burned during the workout (in kilocalories)
    /// </summary>
    public double? EnergyBurned { get; init; }

    /// <summary>
    /// Total distance covered during the workout (in meters)
    /// </summary>
    public double? Distance { get; init; }

    /// <summary>
    /// Average heart rate during the workout (BPM)
    /// </summary>
    public double? AverageHeartRate { get; init; }

    /// <summary>
    /// Maximum heart rate during the workout (BPM)
    /// </summary>
    public double? MaxHeartRate { get; init; }

    /// <summary>
    /// Minimum heart rate during the workout (BPM)
    /// </summary>
    public double? MinHeartRate { get; init; }
}
