namespace Maui.Health.Models;

/// <summary>
/// Represents a date/time range with an optional end time
/// </summary>
public class DateRange
{
    /// <summary>
    /// Start time of the range
    /// </summary>
    public DateTimeOffset Start { get; }

    /// <summary>
    /// End time of the range (null if ongoing)
    /// </summary>
    public DateTimeOffset? End { get; private set; }

    /// <summary>
    /// Whether this range has been closed (has an end time)
    /// </summary>
    public bool IsClosed => End.HasValue;

    /// <summary>
    /// Gets the duration of this range. If not closed, calculates duration until now.
    /// </summary>
    public TimeSpan Duration => (End ?? DateTimeOffset.UtcNow) - Start;

    public DateRange(DateTimeOffset start, DateTimeOffset? end = null)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// Closes the range by setting the end time
    /// </summary>
    /// <param name="endTime">The end time to set</param>
    public void Close(DateTimeOffset endTime)
    {
        End = endTime;
    }
}
