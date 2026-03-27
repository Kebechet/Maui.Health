using Maui.Health.Enums;

namespace Maui.Health.Models;

/// <summary>
/// Represents a single change (upsert or deletion) detected via differential sync.
/// </summary>
public class HealthChange
{
    /// <summary>
    /// Whether the record was upserted or deleted.
    /// </summary>
    public required HealthChangeType Type { get; init; }

    /// <summary>
    /// The platform-specific record ID that was changed.
    /// </summary>
    public required string RecordId { get; init; }

    /// <summary>
    /// The health data type of the changed record.
    /// Only populated for upsert changes on some platforms.
    /// </summary>
    public HealthDataType? DataType { get; init; }
}
