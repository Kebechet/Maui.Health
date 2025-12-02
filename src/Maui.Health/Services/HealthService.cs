using Maui.Health.Models;
using Maui.Health.Models.Metrics;
using Microsoft.Extensions.Logging;

namespace Maui.Health.Services;

public partial class HealthService : IHealthService
{
    public HealthWorkoutService Activity { get; }
    protected readonly ILogger<HealthService> _logger;

    public HealthService(HealthWorkoutService activityService, ILogger<HealthService> logger)
    {
        Activity = activityService;
        _logger = logger;

        // Set up callbacks for ActivityService to fetch health data
        Activity.HeartRateQueryCallback = (timeRange, ct) => GetHealthData<HeartRateDto>(timeRange, ct);
        Activity.CaloriesQueryCallback = (timeRange, ct) => GetHealthData<ActiveCaloriesBurnedDto>(timeRange, ct);
    }

    public partial bool IsSupported { get; }

    public Task<RequestPermissionResult> RequestPermission(HealthPermissionDto healthPermission, bool canRequestFullHistoryPermission = false, CancellationToken cancellationToken = default)
    {
        return RequestPermissions([healthPermission]);
    }

    public partial Task<RequestPermissionResult> RequestPermissions(IList<HealthPermissionDto> healthPermissions, bool canRequestFullHistoryPermission = false, CancellationToken cancellationToken = default);

    public partial Task<List<TDto>> GetHealthData<TDto>(HealthTimeRange timeRange, CancellationToken cancellationToken = default)
        where TDto : HealthMetricBase;

    public partial Task<bool> WriteHealthData<TDto>(TDto data, CancellationToken cancellationToken = default)
        where TDto : HealthMetricBase;
}
