using Maui.Health.Models;
using Maui.Health.Models.Metrics;

namespace Maui.Health.Services;

public partial class HealthService : IHealthService
{
    public partial bool IsSupported => false;

    public partial Task<RequestPermissionResult> RequestPermissions(IList<HealthPermissionDto> healthPermissions, bool canRequestFullHistoryPermission, CancellationToken cancellationToken)
    {
        return Task.FromResult(new RequestPermissionResult());
    }

    public partial Task<List<TDto>> GetHealthData<TDto>(HealthTimeRange timeRange, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        return Task.FromResult<List<TDto>>([]);
    }

    public partial Task<bool> WriteHealthData<TDto>(TDto data, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        return Task.FromResult(false);
    }

    public partial void OpenHealthStoreForUpdate() { }
}
