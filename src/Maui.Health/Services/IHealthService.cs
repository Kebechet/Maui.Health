using Maui.Health.Models;
using Maui.Health.Models.Metrics;
using Maui.Health.Models.Requests;
using Maui.Health.Models.Responses;

namespace Maui.Health.Services;

public interface IHealthService
{
    /// <summary>
    /// Get health data for a specific metric type within a time range
    /// </summary>
    /// <typeparam name="TDto">The type of health metric DTO to retrieve</typeparam>
    /// <param name="timeRange">The time range for data retrieval</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Array of health metric DTOs</returns>
    Task<List<TDto>> GetHealthDataAsync<TDto>(
        HealthTimeRange timeRange,
        CancellationToken cancellationToken = default)
        where TDto : HealthMetricBase;

    Task<ReadHealthDataResponse<TDto>> GetHealthDataAsync<TDto>(
        ReadHealthDataRequest request,
        CancellationToken cancellationToken = default) where TDto : HealthMetricBase;

    Task<GetChangesResponse<TDto>> GetHealthDataChangesAsync<TDto>(
        GetChangesRequest request,
        CancellationToken cancellationToken) where TDto : HealthMetricBase;

    Task<RequestPermissionResult> RequestPermission(
        HealthPermissionDto healthPermission,
        bool canRequestReadInBackgroundPermission = false,
        bool canRequestFullHistoryPermission = false,
        CancellationToken cancellationToken = default);
    Task<RequestPermissionResult> RequestPermissions(
        IList<HealthPermissionDto> healthPermissions,
        bool canRequestReadInBackgroundPermission = false,
        bool canRequestFullHistoryPermission = false,
        CancellationToken cancellationToken = default);

    Task<RequestPermissionResult> CheckPermissionStatusAsync(
        IList<HealthPermissionDto> healthPermissions,
        bool canRequestReadInBackgroundPermission = false,
        bool canRequestFullHistoryPermission = false,
        CancellationToken cancellationToken = default);

}
