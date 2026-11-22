using Kebechet.Types.Health.Enums;
using Kebechet.Types.Health.Enums.Errors;
using Kebechet.Types.Health.Models;
using Kebechet.Types.Health.Models.Metrics;
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
    public Task<RequestPermissionResult> RequestPermission(HealthPermissionDto healthPermission, bool canRequestFullHistoryPermission = false, bool canRequestBackgroundReadPermission = false, CancellationToken cancellationToken = default)
    {
        return RequestPermissions([healthPermission], canRequestFullHistoryPermission, canRequestBackgroundReadPermission, cancellationToken);
    }

    /// <summary>
    /// Serializes permission requests across every instance. Parallel requests on Android each launched their own
    /// request: one screen showed and the rest stayed on top of the app as invisible activities that swallowed every
    /// tap. Queued, a later request asks only for what the earlier one left missing.
    /// </summary>
    private static readonly SemaphoreSlim _permissionRequestGate = new(1, 1);

    /// <inheritdoc/>
    public async Task<RequestPermissionResult> RequestPermissions(IList<HealthPermissionDto> healthPermissions, bool canRequestFullHistoryPermission = false, bool canRequestBackgroundReadPermission = false, CancellationToken cancellationToken = default)
    {
        await _permissionRequestGate.WaitAsync(cancellationToken);
        try
        {
            return await RequestPermissionsInternal(healthPermissions, canRequestFullHistoryPermission, canRequestBackgroundReadPermission, cancellationToken);
        }
        finally
        {
            _permissionRequestGate.Release();
        }
    }

    private partial Task<RequestPermissionResult> RequestPermissionsInternal(IList<HealthPermissionDto> healthPermissions, bool canRequestFullHistoryPermission, bool canRequestBackgroundReadPermission, CancellationToken cancellationToken);

    /// <inheritdoc/>
    public partial Task<IList<HealthPermissionStatusResult>> GetPermissionStatuses(IList<HealthPermissionDto> permissions, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public partial Task<HealthDataReadResult<TDto>> GetHealthData<TDto>(HealthTimeRange timeRange, bool shouldCheckPermissions = true, CancellationToken cancellationToken = default)
        where TDto : HealthMetricBase;

    /// <inheritdoc/>
    public partial Task<HealthRecordReadResult<TDto>> GetHealthRecord<TDto>(string id, bool shouldCheckPermissions = true, CancellationToken cancellationToken = default)
        where TDto : HealthMetricBase;

    /// <inheritdoc/>
    public Task<WriteHealthDataResult> WriteHealthData<TDto>(TDto item, bool shouldCheckPermissions = true, CancellationToken cancellationToken = default)
        where TDto : IHealthWritable
    {
        return WriteHealthData([item], shouldCheckPermissions, cancellationToken);
    }

    /// <inheritdoc/>
    public partial Task<WriteHealthDataResult> WriteHealthData<TDto>(IList<TDto> items, bool shouldCheckPermissions = true, CancellationToken cancellationToken = default)
        where TDto : IHealthWritable;

    /// <inheritdoc/>
    public partial Task<UpdateHealthDataResult> UpdateHealthData<TDto>(string recordId, TDto item, bool shouldCheckPermissions = true, CancellationToken cancellationToken = default)
        where TDto : IHealthWritable;

    /// <inheritdoc/>
    public partial Task<bool> DeleteHealthData<TDto>(string id, bool shouldCheckPermissions = true, CancellationToken cancellationToken = default)
        where TDto : HealthMetricBase;

    /// <inheritdoc/>
    public partial Task<AggregatedReadResult> GetAggregatedHealthData<TDto>(HealthTimeRange timeRange, bool shouldCheckPermissions = true, CancellationToken cancellationToken = default)
        where TDto : HealthMetricBase;

    /// <inheritdoc/>
    public partial Task<AggregatedIntervalReadResult> GetAggregatedHealthDataByInterval<TDto>(HealthTimeRange timeRange, TimeSpan interval, TimeZoneInfo timeZone, bool shouldCheckPermissions = true, CancellationToken cancellationToken = default)
        where TDto : HealthMetricBase;

    /// <inheritdoc/>
    public partial Task<AggregatedIntervalReadResult> GetAggregatedHealthDataByCalendarPeriod<TDto>(HealthTimeRange timeRange, CalendarUnit unit, int count, TimeZoneInfo timeZone, bool shouldCheckPermissions = true, CancellationToken cancellationToken = default)
        where TDto : HealthMetricBase;

    /// <inheritdoc/>
    public partial Task<ChangesTokenResult> GetChangesToken(IList<HealthDataType> dataTypes, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public partial Task<ChangesReadResult> GetChanges(string token, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public partial void OpenStorePageOfHealthProvider();

    /// <inheritdoc/>
    public partial Result<OpenPermissionSettingsError> OpenPermissionSettings();

    /// <inheritdoc/>
    public partial Task<DateTime> GetEarliestAccessibleDateTime(CancellationToken cancellationToken = default);
}
