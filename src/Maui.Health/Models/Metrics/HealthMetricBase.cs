namespace Maui.Health.Models.Metrics;

/// <summary>
/// Base class for all health metric DTOs providing common properties
/// </summary>
public abstract class HealthMetricBase
{
    /// <summary>
    /// Unique identifier for this health record
    /// </summary>
    public required string Id { get; init; }
    
    /// <summary>
    /// Source of the data (app package name, device name, etc.)
    /// </summary>
    public required string DataOrigin { get; init; }
    
    /// <summary>
    /// Timestamp when the measurement was taken or recorded
    /// For duration-based metrics, this typically represents the start time or a representative time
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }
    
    /// <summary>
    /// How the data was recorded (manual, automatic, etc.)
    /// </summary>
    public string? RecordingMethod { get; init; }
    
    /// <summary>
    /// Additional metadata for the record
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}