using Maui.Health.Enums;
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

    public partial Task<IList<HealthPermissionStatusResult>> GetPermissionStatuses(IList<HealthPermissionDto> permissions, CancellationToken cancellationToken)
    {
        IList<HealthPermissionStatusResult> results = permissions
            .Select(p => new HealthPermissionStatusResult
            {
                Permission = p,
                Status = HealthPermissionStatus.NotDetermined
            })
            .ToList();

        return Task.FromResult(results);
    }

    public partial Task<List<TDto>> GetHealthData<TDto>(HealthTimeRange timeRange, bool shouldCheckPermissions, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        return Task.FromResult<List<TDto>>([]);
    }

    public partial Task<TDto?> GetHealthRecord<TDto>(string id, bool shouldCheckPermissions, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        return Task.FromResult<TDto?>(null);
    }

    public partial Task<bool> WriteHealthData<TDto>(IList<TDto> items, bool shouldCheckPermissions, CancellationToken cancellationToken)
        where TDto : IHealthWritable
    {
        return Task.FromResult(false);
    }

    public partial Task<bool> DeleteHealthData<TDto>(string id, bool shouldCheckPermissions, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        return Task.FromResult(false);
    }

    public partial Task<AggregatedResult?> GetAggregatedHealthData<TDto>(HealthTimeRange timeRange, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        return Task.FromResult<AggregatedResult?>(null);
    }

    public partial Task<List<AggregatedResult>> GetAggregatedHealthDataByInterval<TDto>(HealthTimeRange timeRange, TimeSpan interval, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        return Task.FromResult<List<AggregatedResult>>([]);
    }

    public partial Task<string?> GetChangesToken(IList<HealthDataType> dataTypes, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(null);
    }

    public partial Task<HealthChangesResult?> GetChanges(string token, CancellationToken cancellationToken)
    {
        return Task.FromResult<HealthChangesResult?>(null);
    }

    public partial void OpenStorePageOfHealthProvider() { }
}
