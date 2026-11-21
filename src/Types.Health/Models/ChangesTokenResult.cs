using Maui.Health.Enums;

namespace Maui.Health.Models;

/// <summary>
/// Result of <c>IHealthService.GetChangesToken</c>.
/// </summary>
/// <remarks>
/// On success, <see cref="Token"/> is the opaque token the caller must persist and use on
/// the next <c>IHealthService.GetChanges</c>
/// call. On failure, <see cref="Result.IsError"/> is <c>true</c> and <see cref="Token"/> is
/// null. Typical failures: permission denial, SDK unavailable, platform query error.
/// </remarks>
public class ChangesTokenResult : Result
{
    /// <summary>
    /// The opaque changes token assigned by the platform. Null on failure.
    /// </summary>
    public string? Token { get; init; }
}
