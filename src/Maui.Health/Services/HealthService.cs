using Maui.Health.Enums;
using Maui.Health.Models;
using Maui.Health.Models.Metrics;
using Microsoft.Extensions.Logging;

namespace Maui.Health.Services;

/// <inheritdoc/>
public partial class HealthService : IHealthService
{
    /// <inheritdoc/>
    public IHealthWorkoutService Activity => _activityService;

    private readonly HealthWorkoutService _activityService;
    private readonly ILogger<HealthService> _logger;

    /// <inheritdoc/>
    public partial bool IsSupported { get; }

    /// <inheritdoc/>
    public HealthService(HealthWorkoutService activityService, ILogger<HealthService> logger)
    {
        _activityService = activityService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<RequestPermissionResult> RequestPermission(HealthPermissionDto healthPermission, bool canRequestFullHistoryPermission = false, CancellationToken cancellationToken = default)
    {
        return RequestPermissions([healthPermission], canRequestFullHistoryPermission, cancellationToken);
    }

    /// <inheritdoc/>
    public partial Task<RequestPermissionResult> RequestPermissions(IList<HealthPermissionDto> healthPermissions, bool canRequestFullHistoryPermission = false, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public partial Task<IList<HealthPermissionStatusResult>> GetPermissionStatuses(IList<HealthPermissionDto> permissions, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public partial Task<List<TDto>> GetHealthData<TDto>(HealthTimeRange timeRange, bool shouldCheckPermissions = true, CancellationToken cancellationToken = default)
        where TDto : HealthMetricBase;

    /// <inheritdoc/>
    public partial Task<TDto?> GetHealthRecord<TDto>(string id, bool shouldCheckPermissions = true, CancellationToken cancellationToken = default)
        where TDto : HealthMetricBase;

    /// <inheritdoc/>
    public Task<bool> WriteHealthData<TDto>(TDto item, bool shouldCheckPermissions = true, CancellationToken cancellationToken = default)
        where TDto : IHealthWritable
    {
        return WriteHealthData([item], shouldCheckPermissions, cancellationToken);
    }

    /// <inheritdoc/>
    public partial Task<bool> WriteHealthData<TDto>(IList<TDto> items, bool shouldCheckPermissions = true, CancellationToken cancellationToken = default)
        where TDto : IHealthWritable;

    /// <inheritdoc/>
    public partial Task<bool> DeleteHealthData<TDto>(string id, bool shouldCheckPermissions = true, CancellationToken cancellationToken = default)
        where TDto : HealthMetricBase;

    /// <inheritdoc/>
    public partial Task<AggregatedResult?> GetAggregatedHealthData<TDto>(HealthTimeRange timeRange, CancellationToken cancellationToken = default)
        where TDto : HealthMetricBase;

    /// <inheritdoc/>
    public partial Task<List<AggregatedResult>> GetAggregatedHealthDataByInterval<TDto>(HealthTimeRange timeRange, TimeSpan interval, CancellationToken cancellationToken = default)
        where TDto : HealthMetricBase;

    /// <inheritdoc/>
    public partial Task<string?> GetChangesToken(IList<HealthDataType> dataTypes, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public partial Task<HealthChangesResult?> GetChanges(string token, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public partial void OpenStorePageOfHealthProvider();

    /// <inheritdoc/>
    public partial Task<DateTime> GetEarliestAccessibleDateTime(CancellationToken cancellationToken = default);
}
