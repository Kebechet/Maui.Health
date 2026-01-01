using Maui.Health.Models;
using Maui.Health.Models.Metrics;
using Maui.Health.Models.Requests;
using Maui.Health.Models.Responses;
using Microsoft.Extensions.Logging;

namespace Maui.Health.Services;

public partial class HealthService : IHealthService
{
    private readonly ILogger<HealthService> _logger;

    public HealthService(ILogger<HealthService> logger)
    {
        _logger = logger;
    }

    public partial bool IsSupported { get; }

    public partial Task<RequestPermissionResult> CheckPermissionStatusAsync(
        IList<HealthPermissionDto> healthPermissions,
        bool canRequestReadInBackgroundPermission = false,
        bool canRequestFullHistoryPermission = false,
        CancellationToken cancellationToken = default);

    public Task<RequestPermissionResult> RequestPermission(
        HealthPermissionDto healthPermission,
        bool canRequestReadInBackgroundPermission = false,
        bool canRequestFullHistoryPermission = false,
        CancellationToken cancellationToken = default)
    {
        return RequestPermissions(
            [healthPermission],
            canRequestReadInBackgroundPermission,
            canRequestFullHistoryPermission,
            cancellationToken);
    }

    public partial Task<RequestPermissionResult> RequestPermissions(
        IList<HealthPermissionDto> healthPermissions,
        bool canRequestReadInBackgroundPermission = false,
        bool canRequestFullHistoryPermission = false,
        CancellationToken cancellationToken = default);

    public partial Task<List<TDto>> GetHealthDataAsync<TDto>(
        HealthTimeRange timeRange,
        CancellationToken cancellationToken = default)
        where TDto : HealthMetricBase;

    public partial Task<ReadHealthDataResponse<TDto>> GetHealthDataAsync<TDto>(
        ReadHealthDataRequest request,
        CancellationToken cancellationToken = default) where TDto : HealthMetricBase;

    public partial Task<GetChangesResponse<TDto>> GetHealthDataChangesAsync<TDto>(
        GetChangesRequest request,
        CancellationToken cancellationToken) where TDto : HealthMetricBase;
}
