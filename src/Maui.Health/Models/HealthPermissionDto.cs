using Maui.Health.Enums;

namespace Maui.Health.Models;

public record HealthPermissionDto
{
    public required HealthDataType HealthDataType { get; init; }
    public required PermissionType PermissionType { get; init; }
}
