using Maui.Health.Models;
using Maui.Health.Models.Metrics;

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
    Task<TDto[]> GetHealthDataAsync<TDto>(HealthTimeRange timeRange, CancellationToken cancellationToken = default)
        where TDto : HealthMetricBase;

    /// <summary>
    /// Write health data to the health store
    /// </summary>
    /// <typeparam name="TDto">The type of health metric DTO to write</typeparam>
    /// <param name="data">The health data to write</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> WriteHealthDataAsync<TDto>(TDto data, CancellationToken cancellationToken = default)
        where TDto : HealthMetricBase;

    Task<RequestPermissionResult> RequestPermission(HealthPermissionDto healthPermission, bool canRequestFullHistoryPermission = false, CancellationToken cancellationToken = default);
    Task<RequestPermissionResult> RequestPermissions(IList<HealthPermissionDto> healthPermissions, bool canRequestFullHistoryPermission = false, CancellationToken cancellationToken = default);
}
