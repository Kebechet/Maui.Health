using Maui.Health.Models;

namespace Maui.Health.Extensions;

/// <summary>
/// Extension methods for DateRange parsing and serialization
/// </summary>
public static class DateRangeExtensions
{
    /// <summary>
    /// Serializes a list of DateRange objects to JSON string
    /// </summary>
    /// <param name="intervals">List of DateRange objects to serialize</param>
    /// <returns>JSON string representation</returns>
    public static string ToJson(this List<DateRange> intervals)
    {
        var serialized = intervals
            .Select(i => new
            {
                Start = i.Start.ToUnixTimeMilliseconds(),
                End = i.End?.ToUnixTimeMilliseconds()
            })
            .ToList();

        return System.Text.Json.JsonSerializer.Serialize(serialized);
    }

    /// <summary>
    /// Parses a JSON string to a list of DateRange objects
    /// </summary>
    /// <param name="json">JSON string containing serialized date ranges</param>
    /// <returns>List of DateRange objects</returns>
    public static List<DateRange> ParseDateRanges(this string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return [];
        }

        try
        {
            var intervals = System.Text.Json.JsonSerializer.Deserialize<List<(long, long?)>>(json);
            if (intervals is null)
            {
                return [];
            }

            return intervals.Select(i => new DateRange(
                DateTimeOffset.FromUnixTimeMilliseconds(i.Item1),
                i.Item2.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(i.Item2.Value) : null
            )).ToList();
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Deserializes an object (from metadata) to a list of DateRange objects
    /// </summary>
    /// <param name="intervalsObj">Object containing serialized date ranges</param>
    /// <returns>List of DateRange objects</returns>
    public static List<DateRange> ToDateRanges(this object intervalsObj)
    {
        try
        {
            var json = intervalsObj.ToString();
            if (string.IsNullOrEmpty(json))
            {
                return [];
            }

            var intervals = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, long?>>>(json);
            if (intervals is null)
            {
                return [];
            }

            return intervals
                .Select(i =>
                {
                    var start = i.TryGetValue("Start", out var startMs) && startMs.HasValue
                        ? DateTimeOffset.FromUnixTimeMilliseconds(startMs.Value)
                        : DateTimeOffset.UtcNow;

                    var end = i.TryGetValue("End", out var endMs) && endMs.HasValue
                        ? DateTimeOffset.FromUnixTimeMilliseconds(endMs.Value)
                        : (DateTimeOffset?)null;

                    return new DateRange(start, end);
                })
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}
