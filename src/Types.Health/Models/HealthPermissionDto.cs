using Kebechet.Types.Health.Enums;

namespace Kebechet.Types.Health.Models;

/// <summary>
/// Describes a health data permission request (data type + read/write).
/// </summary>
public record HealthPermissionDto
{
    private const PermissionType ReadAndWrite = PermissionType.Read | PermissionType.Write;

    private readonly PermissionType _permissionType;

    /// <summary>
    /// The type of health data to request permission for.
    /// </summary>
    public required HealthDataType HealthDataType { get; init; }

    /// <summary>
    /// Whether to request read or write permission, or both. A value with neither flag set, or with any other bit set,
    /// throws: it would request nothing and fail silently.
    /// </summary>
    public required PermissionType PermissionType
    {
        get => _permissionType;
        init
        {
            if ((value & ReadAndWrite) == 0 || (value & ~ReadAndWrite) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(PermissionType), value, $"Set {nameof(PermissionType.Read)}, {nameof(PermissionType.Write)} or both.");
            }

            _permissionType = value;
        }
    }

    /// <summary>
    /// This permission as one permission per access: a permission for read and write becomes a read and a write.
    /// The platforms grant reading and writing separately, so statuses are reported per access.
    /// </summary>
    public IEnumerable<HealthPermissionDto> ToSingleAccessPermissions()
    {
        if (PermissionType.HasFlag(PermissionType.Read))
        {
            yield return this with { PermissionType = PermissionType.Read };
        }

        if (PermissionType.HasFlag(PermissionType.Write))
        {
            yield return this with { PermissionType = PermissionType.Write };
        }
    }
}
