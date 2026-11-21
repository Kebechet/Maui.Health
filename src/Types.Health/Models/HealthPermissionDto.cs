using Maui.Health.Enums;

namespace Maui.Health.Models;

/// <summary>
/// Describes a health data permission request (data type + read/write).
/// </summary>
public record HealthPermissionDto
{
    /// <summary>
    /// The type of health data to request permission for.
    /// </summary>
    public required HealthDataType HealthDataType { get; init; }
    /// <summary>
    /// Whether to request read or write permission.
    /// </summary>
    public required PermissionType PermissionType { get; init; }
}
