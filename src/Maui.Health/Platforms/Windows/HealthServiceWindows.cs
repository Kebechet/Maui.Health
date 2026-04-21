using Maui.Health.Enums;
using Maui.Health.Enums.Errors;
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

    public partial Task<HealthDataReadResult<TDto>> GetHealthData<TDto>(HealthTimeRange timeRange, bool shouldCheckPermissions, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        return Task.FromResult(new HealthDataReadResult<TDto>
        {
            ErrorException = new PlatformNotSupportedException("Health data is not supported on Windows."),
        });
    }

    public partial Task<HealthRecordReadResult<TDto>> GetHealthRecord<TDto>(string id, bool shouldCheckPermissions, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        return Task.FromResult(new HealthRecordReadResult<TDto>
        {
            ErrorException = new PlatformNotSupportedException("Health data is not supported on Windows."),
        });
    }

    public partial Task<WriteHealthDataResult> WriteHealthData<TDto>(IList<TDto> items, bool shouldCheckPermissions, CancellationToken cancellationToken)
        where TDto : IHealthWritable
    {
        return Task.FromResult(new WriteHealthDataResult { Error = WriteHealthDataError.NotSupported });
    }

    public partial Task<UpdateHealthDataResult> UpdateHealthData<TDto>(string recordId, TDto item, bool shouldCheckPermissions, CancellationToken cancellationToken)
        where TDto : IHealthWritable
    {
        return Task.FromResult(new UpdateHealthDataResult { Error = UpdateHealthDataError.NotSupported });
    }

    public partial Task<bool> DeleteHealthData<TDto>(string id, bool shouldCheckPermissions, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        return Task.FromResult(false);
    }

    public partial Task<AggregatedReadResult> GetAggregatedHealthData<TDto>(HealthTimeRange timeRange, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        return Task.FromResult(new AggregatedReadResult
        {
            ErrorException = new PlatformNotSupportedException("Health data is not supported on Windows."),
        });
    }

    public partial Task<AggregatedIntervalReadResult> GetAggregatedHealthDataByInterval<TDto>(HealthTimeRange timeRange, TimeSpan interval, CancellationToken cancellationToken)
        where TDto : HealthMetricBase
    {
        return Task.FromResult(new AggregatedIntervalReadResult
        {
            ErrorException = new PlatformNotSupportedException("Health data is not supported on Windows."),
        });
    }

    public partial Task<ChangesTokenResult> GetChangesToken(IList<HealthDataType> dataTypes, CancellationToken cancellationToken)
    {
        return Task.FromResult(new ChangesTokenResult
        {
            ErrorException = new PlatformNotSupportedException("Health data is not supported on Windows."),
        });
    }

    public partial Task<ChangesReadResult> GetChanges(string token, CancellationToken cancellationToken)
    {
        return Task.FromResult(new ChangesReadResult
        {
            ErrorException = new PlatformNotSupportedException("Health data is not supported on Windows."),
        });
    }

    public partial void OpenStorePageOfHealthProvider() { }

    public partial Task<DateTime> GetEarliestAccessibleDateTime(CancellationToken cancellationToken)
    {
        return Task.FromResult(DateTime.MinValue);
    }
}
