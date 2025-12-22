using Maui.Health.Enums;

namespace Maui.Health.Models.Metrics;

/// <summary>
/// Represents a workout/exercise session from health platforms.
/// Standalone class for workout manipulation, independent of HealthMetricBase.
/// </summary>
public class WorkoutDto
{
    /// <summary>
    /// Unique identifier for this workout record
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Source of the data (app package name, device name, etc.)
    /// </summary>
    public required string DataOrigin { get; init; }

    /// <summary>
    /// Timestamp when the workout was recorded (typically same as StartTime)
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// How the data was recorded (manual, automatic, etc.)
    /// </summary>
    public string? RecordingMethod { get; init; }

    /// <summary>
    /// Additional metadata for the workout record
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

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
    /// End time of the workout session (null if session is still active)
    /// </summary>
    public DateTimeOffset? EndTime { get; init; }

    /// <summary>
    /// Duration of the workout in seconds (calculated from current time if EndTime is null)
    /// </summary>
    public double DurationSeconds => ((EndTime ?? DateTimeOffset.UtcNow) - StartTime).TotalSeconds;

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
