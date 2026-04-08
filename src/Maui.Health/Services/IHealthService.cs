using System.Diagnostics.CodeAnalysis;
using Maui.Health.Enums;
using Maui.Health.Models;
using Maui.Health.Models.Metrics;

namespace Maui.Health.Services;

/// <summary>
/// Cross-platform service for reading and writing health data.
/// On Android it uses Health Connect, on iOS it uses HealthKit.
/// </summary>
public interface IHealthService
{
    /// <summary>
    /// Whether the health data platform is available on this device.
    /// On Android: checks Health Connect SDK availability.
    /// On iOS: always true (HealthKit is built into the OS).
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Access to workout/activity tracking service
    /// </summary>
    IHealthWorkoutService Activity { get; }

    /// <summary>
    /// Request a single health permission
    /// </summary>
    Task<RequestPermissionResult> RequestPermission(HealthPermissionDto healthPermission, bool canRequestFullHistoryPermission = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Request multiple health permissions.
    /// On iOS: the result only reflects write permission grants. Read permission denials are not detectable
    /// due to Apple's privacy design — see <see cref="RequestPermissionResult"/> for details.
    /// For a per-permission breakdown, use <see cref="GetPermissionStatuses"/> after this call.
    /// </summary>
    Task<RequestPermissionResult> RequestPermissions(IList<HealthPermissionDto> healthPermissions, bool canRequestFullHistoryPermission = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current authorization status of the specified health permissions without triggering the permission UI.
    /// On Android: accurately reports both read and write permission status.
    /// On iOS: accurately reports write permission status. Read permissions always return <see cref="Enums.HealthPermissionStatus.NotDetermined"/>
    /// because Apple does not expose read authorization status.
    /// </summary>
    /// <param name="permissions">The permissions to check status for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A list of results pairing each input permission with its current status</returns>
    Task<IList<HealthPermissionStatusResult>> GetPermissionStatuses(IList<HealthPermissionDto> permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get health data for a specific metric type within a time range
    /// </summary>
    /// <typeparam name="TDto">The type of health metric DTO to retrieve</typeparam>
    /// <param name="timeRange">The time range for data retrieval</param>
    /// <param name="shouldCheckPermissions">When false, skips the internal permission check. Use when permissions were already requested upfront via <see cref="RequestPermissions"/>.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of health metric DTOs</returns>
    Task<List<TDto>> GetHealthData<TDto>(HealthTimeRange timeRange, bool shouldCheckPermissions = true, CancellationToken cancellationToken = default)
        where TDto : HealthMetricBase;

    /// <summary>
    /// Get a single health record by its platform-specific ID.
    /// On Android: uses Health Connect record metadata ID.
    /// On iOS: uses HealthKit UUID.
    /// </summary>
    /// <typeparam name="TDto">The type of health metric DTO to retrieve</typeparam>
    /// <param name="id">The platform-specific record ID</param>
    /// <param name="shouldCheckPermissions">When false, skips the internal permission check. Use when permissions were already requested upfront via <see cref="RequestPermissions"/>.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The health record, or null if not found</returns>
    [Experimental("MH001")]
    Task<TDto?> GetHealthRecord<TDto>(string id, bool shouldCheckPermissions = true, CancellationToken cancellationToken = default)
        where TDto : HealthMetricBase;

    /// <summary>
    /// Write health data to the health store
    /// </summary>
    /// <typeparam name="TDto">The type of health metric DTO to write</typeparam>
    /// <param name="data">The health data to write</param>
    /// <param name="shouldCheckPermissions">When false, skips the internal permission check. Use when permissions were already requested upfront via <see cref="RequestPermissions"/>.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> WriteHealthData<TDto>(TDto data, bool shouldCheckPermissions = true, CancellationToken cancellationToken = default)
        where TDto : IHealthWritable;

    /// <summary>
    /// Delete a health record by its platform-specific ID.
    /// You can only delete records that were created by your application.
    /// </summary>
    /// <typeparam name="TDto">The type of health metric DTO to delete</typeparam>
    /// <param name="id">The platform-specific record ID</param>
    /// <param name="shouldCheckPermissions">When false, skips the internal permission check. Use when permissions were already requested upfront via <see cref="RequestPermissions"/>.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false otherwise</returns>
    [Experimental("MH002")]
    Task<bool> DeleteHealthData<TDto>(string id, bool shouldCheckPermissions = true, CancellationToken cancellationToken = default)
        where TDto : HealthMetricBase;

    /// <summary>
    /// Get aggregated health data for a time range.
    /// Uses platform-native aggregation (Android aggregate(), iOS HKStatisticsQuery)
    /// which properly deduplicates data across multiple health apps.
    /// </summary>
    /// <param name="timeRange">The time range to aggregate over</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The aggregated result, or null if no data or unsupported type</returns>
    [Experimental("MH003")]
    Task<AggregatedResult?> GetAggregatedHealthData<TDto>(HealthTimeRange timeRange, CancellationToken cancellationToken = default)
        where TDto : HealthMetricBase;

    /// <summary>
    /// Get aggregated health data bucketed by a time interval.
    /// Uses platform-native aggregation (Android aggregateGroupByDuration(), iOS HKStatisticsCollectionQuery)
    /// which properly deduplicates data across multiple health apps.
    /// </summary>
    /// <param name="timeRange">The overall time range to aggregate</param>
    /// <param name="interval">The bucket interval (e.g., TimeSpan.FromDays(1) for daily)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of aggregated results, one per interval bucket</returns>
    [Experimental("MH004")]
    Task<List<AggregatedResult>> GetAggregatedHealthDataByInterval<TDto>(HealthTimeRange timeRange, TimeSpan interval, CancellationToken cancellationToken = default)
        where TDto : HealthMetricBase;

    /// <summary>
    /// Get a token for tracking changes to specific health data types.
    /// Store this token persistently - it is needed for subsequent GetChanges calls.
    /// Tokens expire after 30 days.
    /// </summary>
    /// <param name="dataTypes">The health data types to track changes for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An opaque token string, or null if the operation failed</returns>
    [Experimental("MH005")]
    Task<string?> GetChangesToken(IList<HealthDataType> dataTypes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get changes (upserts and deletions) since the provided token.
    /// If HasMore is true, call again with NextToken to get remaining changes.
    /// </summary>
    /// <param name="token">The token from GetChangesToken or a previous GetChanges call</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The changes result, or null if the token is invalid/expired</returns>
    [Experimental("MH006")]
    Task<HealthChangesResult?> GetChanges(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the platform store to update the health provider app.
    /// Call this after showing your custom UI when RequestPermissions returns
    /// <see cref="Enums.Errors.RequestPermissionError.SdkUnavailableProviderUpdateRequired"/>.
    /// On Android: Opens Play Store to update Health Connect.
    /// On iOS: No-op (HealthKit is built into the OS).
    /// </summary>
    void OpenStorePageOfHealthProvider();
}
