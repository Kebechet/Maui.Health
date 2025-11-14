using Maui.Health.Models;
using Maui.Health.Models.Metrics;

namespace Maui.Health.Services;

public partial class HealthService : IHealthService
{
    public partial bool IsSupported { get; }

    public Task<RequestPermissionResult> RequestPermission(HealthPermissionDto healthPermission, bool canRequestFullHistoryPermission = false, CancellationToken cancellationToken = default)
    {
        return RequestPermissions([healthPermission]);
    }

    public partial Task<RequestPermissionResult> RequestPermissions(IList<HealthPermissionDto> healthPermissions, bool canRequestFullHistoryPermission = false, CancellationToken cancellationToken = default);

    public partial Task<TDto[]> GetHealthDataAsync<TDto>(DateTime from, DateTime to, CancellationToken cancellationToken = default)
        where TDto : HealthMetricBase;
}
