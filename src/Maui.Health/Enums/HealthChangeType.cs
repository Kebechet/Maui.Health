namespace Maui.Health.Enums;

/// <summary>
/// Type of change detected in health data differential sync.
/// </summary>
public enum HealthChangeType
{
    /// <summary>
    /// A record was inserted or updated.
    /// </summary>
    Upsert,

    /// <summary>
    /// A record was deleted.
    /// </summary>
    Deletion
}
