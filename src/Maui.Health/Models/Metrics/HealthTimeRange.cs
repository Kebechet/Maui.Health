namespace Maui.Health.Models.Metrics;

/// <summary>
/// Represents a time range for querying health data
/// </summary>
public class HealthTimeRange : IHealthTimeRange
{
    /// <summary>
    /// Start time of the measurement period
    /// </summary>
    public required DateTimeOffset StartTime { get; init; }

    /// <summary>
    /// End time of the measurement period
    /// </summary>
    public required DateTimeOffset EndTime { get; init; }

    /// <summary>
    /// Start time as DateTime (local time)
    /// </summary>
    public DateTime StartDateTime => StartTime.DateTime;

    /// <summary>
    /// End time as DateTime (local time)
    /// </summary>
    public DateTime EndDateTime => EndTime.DateTime;

    /// <summary>
    /// Creates a HealthTimeRange from DateTime values
    /// </summary>
    /// <param name="startTime">Start time of the range</param>
    /// <param name="endTime">End time of the range</param>
    /// <returns>A new HealthTimeRange instance</returns>
    public static HealthTimeRange FromDateTime(DateTime startTime, DateTime endTime)
    {
        return new HealthTimeRange
        {
            StartTime = new DateTimeOffset(startTime),
            EndTime = new DateTimeOffset(endTime)
        };
    }

    /// <summary>
    /// Creates a HealthTimeRange from DateTimeOffset values
    /// </summary>
    /// <param name="startTime">Start time of the range</param>
    /// <param name="endTime">End time of the range</param>
    /// <returns>A new HealthTimeRange instance</returns>
    public static HealthTimeRange FromDateTimeOffset(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        return new HealthTimeRange
        {
            StartTime = startTime,
            EndTime = endTime
        };
    }
}
