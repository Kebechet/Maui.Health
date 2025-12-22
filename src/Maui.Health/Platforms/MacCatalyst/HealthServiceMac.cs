using Maui.Health.Models;
using Maui.Health.Models.Metrics;
using Maui.Health.Models.Requests;
using Maui.Health.Models.Responses;

namespace Maui.Health.Services;

public partial class HealthService
{
    public partial bool IsSupported => false;

    public partial Task<RequestPermissionResult> CheckPermissionStatusAsync(IList<HealthPermissionDto> healthPermissions, bool canRequestReadInBackgroundPermission, bool canRequestFullHistoryPermission, CancellationToken cancellationToken)
    {
        return Task.FromResult(new RequestPermissionResult());
    }

    public partial Task<RequestPermissionResult> RequestPermissions(IList<HealthPermissionDto> healthPermissions, bool canRequestReadInBackgroundPermission, bool canRequestFullHistoryPermission, CancellationToken cancellationToken)
    {
        return Task.FromResult(new RequestPermissionResult());
    }

    public partial Task<List<TDto>> GetHealthDataAsync<TDto>(HealthTimeRange timeRange, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        return Task.FromResult(new List<TDto>());
    }

    public partial Task<ReadHealthDataResponse<TDto>> GetHealthDataAsync<TDto>(ReadHealthDataRequest request,
        CancellationToken cancellationToken) where TDto : HealthMetricBase
    {
        return Task.FromResult(new ReadHealthDataResponse<TDto>());
    }

    public partial Task<GetChangesResponse<TDto>> GetHealthDataChangesAsync<TDto>(GetChangesRequest request,
        CancellationToken cancellationToken) where TDto : HealthMetricBase
    {
        return Task.FromResult(new GetChangesResponse<TDto>());
    }
}
