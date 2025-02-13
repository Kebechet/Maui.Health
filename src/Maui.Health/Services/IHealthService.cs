using Maui.Health.Models;

namespace Maui.Health.Services;

public interface IHealthService
{
    Task<long?> GetStepsTodayAsync(CancellationToken cancellationToken = default);
    Task<RequestPermissionResult> RequestPermission(HealthPermissionDto healthPermission, bool canRequestFullHistoryPermission = false, CancellationToken cancellationToken = default);
    Task<RequestPermissionResult> RequestPermissions(IList<HealthPermissionDto> healthPermissions, bool canRequestFullHistoryPermission = false, CancellationToken cancellationToken = default);
}
