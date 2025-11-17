using Maui.Health.Models;
using Maui.Health.Models.Metrics;

namespace Maui.Health.Services;

public partial class HealthService
{
    public partial bool IsSupported => false;

    public partial Task<RequestPermissionResult> RequestPermissions(IList<HealthPermissionDto> healthPermissions, bool canRequestFullHistoryPermission, CancellationToken cancellationToken)
    {
        return Task.FromResult(new RequestPermissionResult());
    }

    public partial Task<TDto[]> GetHealthDataAsync<TDto>(HealthTimeRange timeRange, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        return Task.FromResult(Array.Empty<TDto>());
    }

    public partial Task<bool> WriteHealthDataAsync<TDto>(TDto data, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        return Task.FromResult(false);
    }
}
