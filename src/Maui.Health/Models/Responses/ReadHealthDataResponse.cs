namespace Maui.Health.Models.Responses;

public class ReadHealthDataResponse<TDto>
{
    public List<TDto> Records { get; set; } = new();
    public object? PageTokenOrAnchor { get; set; }

    /// <summary>
    /// For Android this will be true if the rate limit was exceeded.
    /// Inspect the RateLimitLastDateTimeOffsetUtc to see when this last occurred.
    /// Your app should wait and not call the service again until the rate limit resets.
    /// See Android documentation for more details.
    /// https://developer.android.com/health-and-fitness/health-connect/rate-limiting
    /// </summary>
    public bool IsRateExceeded { get; set; }

    public DateTimeOffset? RateLimitLastDateTimeOffsetUtc { get; set; }
    public bool IsError { get; set; }
    public Exception? ErrorException { get; set; }
}