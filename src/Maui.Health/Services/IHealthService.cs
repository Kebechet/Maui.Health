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
    /// Write a single health record to the health store.
    /// Delegates to <see cref="WriteHealthData{TDto}(IList{TDto}, bool, CancellationToken)"/>.
    /// </summary>
    Task<WriteHealthDataResult> WriteHealthData<TDto>(TDto item, bool shouldCheckPermissions = true, CancellationToken cancellationToken = default)
        where TDto : IHealthWritable;

    /// <summary>
    /// Write multiple health records to the health store in a single platform call and
    /// return their platform-assigned IDs. Use the IDs to link an app-authored record to
    /// its native counterpart immediately, without reconciling on a subsequent read cycle.
    /// </summary>
    /// <remarks>
    /// <para>Ordering contract: on success,
    /// <see cref="WriteHealthDataResult.RecordIds"/> is 1:1 with <paramref name="items"/> — that
    /// is, <c>RecordIds[i]</c> is the native ID assigned to <c>items[i]</c>.</para>
    /// <para>On failure, <see cref="WriteHealthDataResult.RecordIds"/> is empty and the typed
    /// <see cref="Models.Result{TError}.Error"/> identifies the failure mode (SDK unavailable,
    /// permission denied, DTO conversion failure, platform-write failure, or an unexpected
    /// exception available on <see cref="Models.Result{TError}.ErrorException"/>). Callers that
    /// only care about success/failure can check <see cref="Models.Result{TError}.IsSuccess"/>.</para>
    /// <para>Windows and macOS Catalyst stubs always return
    /// <see cref="Enums.Errors.WriteHealthDataError.NotSupported"/>.</para>
    /// </remarks>
    /// <typeparam name="TDto">The type of health metric DTO to write</typeparam>
    /// <param name="items">The health data records to write</param>
    /// <param name="shouldCheckPermissions">When false, skips the internal permission check. Use when permissions were already requested upfront via <see cref="RequestPermissions"/>.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<WriteHealthDataResult> WriteHealthData<TDto>(IList<TDto> items, bool shouldCheckPermissions = true, CancellationToken cancellationToken = default)
        where TDto : IHealthWritable;

    /// <summary>
    /// Update an existing health record identified by <paramref name="recordId"/>, replacing
    /// its fields with those of <paramref name="item"/>. Use this to propagate in-app edits
    /// back to the native store without losing the link established by the original write.
    /// </summary>
    /// <remarks>
    /// <para><b>Platform semantics differ — read carefully:</b></para>
    /// <list type="bullet">
    /// <item><description><b>Android</b> (Health Connect): a true in-place update via
    /// <c>updateRecords</c>. The record ID is preserved; on success
    /// <see cref="UpdateHealthDataResult.RecordId"/> equals <paramref name="recordId"/>.</description></item>
    /// <item><description><b>iOS</b> (HealthKit): atomic replacement via
    /// <c>HKMetadataKeySyncIdentifier</c> + <c>HKMetadataKeySyncVersion</c>. A new sample is written
    /// carrying the existing record's sync identifier with a bumped version; HealthKit swaps it in
    /// for the old one in a single save with no delete-then-insert window. HealthKit assigns the
    /// replacement sample a <b>new</b> UUID — <see cref="UpdateHealthDataResult.RecordId"/> holds
    /// the new ID and the caller must re-link their local record to it.</description></item>
    /// </list>
    /// <para><b>iOS-only constraints</b> (both surface as typed
    /// <see cref="Enums.Errors.UpdateHealthDataError"/> values, leaving the original record untouched):</para>
    /// <list type="bullet">
    /// <item><description><see cref="Enums.Errors.UpdateHealthDataError.LegacyRecordNotUpdatable"/> —
    /// the existing sample was written without a sync identifier (by an older SDK version or by a
    /// third-party app that doesn't stamp). Callers that own the record can fall back to
    /// <see cref="DeleteHealthData{TDto}(string, bool, System.Threading.CancellationToken)"/> +
    /// <see cref="WriteHealthData{TDto}(TDto, bool, System.Threading.CancellationToken)"/>.</description></item>
    /// <item><description><see cref="Enums.Errors.UpdateHealthDataError.CrossSourceNotSupported"/> —
    /// the existing sample was authored by a different app. HealthKit scopes sync-identifier replacement
    /// per source bundle, so there is no supported way to update cross-source records.</description></item>
    /// </list>
    /// <para>All failure modes leave the original record untouched.</para>
    /// <para>Windows and macOS Catalyst stubs always return
    /// <see cref="Enums.Errors.UpdateHealthDataError.NotSupported"/>.</para>
    /// </remarks>
    /// <typeparam name="TDto">The type of health metric DTO to write in place of the existing record</typeparam>
    /// <param name="recordId">Native ID of the record to update (from a previous
    /// <see cref="WriteHealthData{TDto}(IList{TDto}, bool, CancellationToken)"/> call
    /// or a fetched record's <c>Id</c>).</param>
    /// <param name="item">The replacement record data</param>
    /// <param name="shouldCheckPermissions">When false, skips the internal permission check. Use when permissions were already requested upfront via <see cref="RequestPermissions"/>.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [Experimental("MH007")]
    Task<UpdateHealthDataResult> UpdateHealthData<TDto>(string recordId, TDto item, bool shouldCheckPermissions = true, CancellationToken cancellationToken = default)
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

    /// <summary>
    /// Returns the earliest UTC <see cref="DateTime"/> from which this device currently allows
    /// historical health-data reads. Callers can use it as the start of their sync window
    /// without having to know per-platform rules.
    /// <para>
    /// On <b>Android</b> (Health Connect): if <c>android.permission.health.READ_HEALTH_DATA_HISTORY</c>
    /// has been granted returns <see cref="DateTime.MinValue"/> (unlimited). Otherwise returns
    /// <c>firstGrantDate - 30 days</c>, where <c>firstGrantDate</c> is the timestamp captured
    /// the first time the user granted any Health Connect permission through this library.
    /// This matches the platform's "30 days prior to when any permission was first granted"
    /// rule. If no anchor is persisted yet (existing install that predates this tracking,
    /// or permissions never granted through the library) we return <c>now - 30 days</c> as a
    /// safe fallback guaranteed to be inside the readable window.
    /// </para>
    /// <para>
    /// On <b>iOS</b> (HealthKit): always returns <see cref="DateTime.MinValue"/>. HealthKit has no
    /// documented lookback cap and authorization grants retroactive read access.
    /// </para>
    /// <para>
    /// On <b>unsupported platforms</b>: returns <see cref="DateTime.MinValue"/>. Read operations
    /// return empty collections on those platforms anyway.
    /// </para>
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The earliest UTC <see cref="DateTime"/> from which reads are currently possible.</returns>
    Task<DateTime> GetEarliestAccessibleDateTime(CancellationToken cancellationToken = default);
}
