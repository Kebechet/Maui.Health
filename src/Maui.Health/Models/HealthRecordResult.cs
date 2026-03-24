using Maui.Health.Enums.Errors;

namespace Maui.Health.Models;

/// <summary>
/// Result of a health record read operation, containing the records or an error.
/// </summary>
public class ReadRecordResult : Result<ReadRecordError>
{
    /// <summary>
    /// The health records returned by the read operation.
    /// </summary>
    public IList<HealthRecord> Records { get; init; } = [];
}
