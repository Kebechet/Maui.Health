using Maui.Health.Models.Metrics;

namespace Maui.Health.Models.Requests;

public class ReadHealthDataRequest
{
    public required HealthTimeRange HealthTimeRange { get; set; }

    /// <summary>
    /// Android PageToken is a string, Apple anchor is an nuint
    /// </summary>
    public object? PageTokenOrAnchor { get; set; }

    /// <summary>
    /// Android max page size is 1000
    /// </summary>
    public int PageSize { get; set; } = 1000;

    public List<string>? OriginFilter { get; set; }

    /// <summary>
    /// Setting this to false means that a page token will be returned if the
    /// number of results in the time range exceeds the page size.
    /// The page token can be used to fetch subsequent pages of results by
    /// passing it in as the PageTokenOrAnchor parameter.
    /// Useful if your UI implements pagination or infinite scrolling.
    /// </summary>
    public bool ReadAll { get; set; } = true;
}