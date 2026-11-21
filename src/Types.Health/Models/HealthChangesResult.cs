namespace Maui.Health.Models;

/// <summary>
/// Result of a differential sync query containing changes since the last token.
/// </summary>
public class HealthChangesResult
{
    /// <summary>
    /// List of changes (upserts and deletions) since the provided token.
    /// </summary>
    public required List<HealthChange> Changes { get; init; }

    /// <summary>
    /// Token to use for the next differential sync call.
    /// Store this persistently between app sessions.
    /// </summary>
    public required string NextToken { get; init; }

    /// <summary>
    /// Whether there are more changes available.
    /// If true, call GetChanges again with NextToken to retrieve more.
    /// </summary>
    public required bool HasMore { get; init; }
}
