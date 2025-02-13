using Maui.Health.Enums;
using Maui.Health.Models;

namespace Maui.Health.Services;

public partial class HealthService : IHealthService
{
    public partial bool IsSupported { get; }

    public Task<RequestPermissionResult> RequestPermission(HealthPermissionDto healthPermission, bool canRequestFullHistoryPermission = false, CancellationToken cancellationToken = default)
    {
        return RequestPermissions([healthPermission]);
    }
    public partial Task<RequestPermissionResult> RequestPermissions(IList<HealthPermissionDto> healthPermissions, bool canRequestFullHistoryPermission = false, CancellationToken cancellationToken = default);
    //public partial Task<ReadRecordResult> ReadRecords(HealthDataType healthDataType, DateTime from, DateTime until, CancellationToken cancellationToken = default);

    public partial Task<long?> GetStepsTodayAsync(CancellationToken cancellationToken = default);
}
