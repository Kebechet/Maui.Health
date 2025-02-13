using Maui.Health.Enums.Errors;

namespace Maui.Health.Models;

public class ReadRecordResult : Result<ReadRecordError>
{
    public IList<HealthRecord> Records { get; init; } = [];
}
