namespace Maui.Health.Enums;

/// <summary>
/// Type of health data permission (read, write, or both via flags).
/// </summary>
[Flags]
public enum PermissionType
{
    /// <summary>
    /// Permission to read health data.
    /// </summary>
    Read,
    /// <summary>
    /// Permission to write health data.
    /// </summary>
    Write
}