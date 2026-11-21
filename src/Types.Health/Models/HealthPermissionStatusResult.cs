using Maui.Health.Enums;

namespace Maui.Health.Models;

/// <summary>
/// Pairs a health permission with its current authorization status on the platform.
/// </summary>
public record HealthPermissionStatusResult
{
    /// <summary>
    /// The health permission that was queried.
    /// </summary>
    public required HealthPermissionDto Permission { get; init; }

    /// <summary>
    /// The current authorization status of the permission.
    /// </summary>
    public required HealthPermissionStatus Status { get; init; }
}
