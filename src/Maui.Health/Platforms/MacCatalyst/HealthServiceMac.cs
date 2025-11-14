using Maui.Health.Models;
using Maui.Health.Models.Metrics;

namespace Maui.Health.Services;

public partial class HealthService
{
    public partial bool IsSupported => false;

    public partial Task<RequestPermissionResult> RequestPermissions(IList<HealthPermissionDto> healthPermissions, bool canRequestFullHistoryPermission = false, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new RequestPermissionResult());
    }

    public partial Task<TDto[]> GetHealthDataAsync<TDto>(HealthTimeRange timeRange, CancellationToken cancellationToken = default)
        where TDto : HealthMetricBase
    {
        return Task.FromResult(Array.Empty<TDto>());
    }
}
