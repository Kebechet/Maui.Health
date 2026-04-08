namespace Maui.Health.Models.Metrics;

/// <summary>
/// Base class for point-in-time health write DTOs.
/// Contains only the fields needed for writing - no read-only metadata like Id, DataSdk, or DataOrigin.
/// </summary>
public abstract class HealthWriteData : IHealthWritable
{
    /// <summary>
    /// Timestamp when the measurement was taken
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Base class for time-range health write DTOs.
/// Contains only the fields needed for writing - no read-only metadata like Id, DataSdk, or DataOrigin.
/// </summary>
public abstract class HealthWriteRangeData : IHealthWritable
{
    /// <summary>
    /// Start time of the measurement period
    /// </summary>
    public required DateTimeOffset StartTime { get; init; }

    /// <summary>
    /// End time of the measurement period
    /// </summary>
    public required DateTimeOffset EndTime { get; init; }
}
