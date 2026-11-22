using Kebechet.Types.Health.Enums;
using Kebechet.Types.Health.Enums.Errors;

namespace Kebechet.Types.Health.Models;

/// <summary>
/// Result of a permission request: the status of every requested permission once the user has answered, the
/// requested data-type permissions that are still not granted, and the extra Android access reported apart from
/// them.
/// <para>
/// <b>iOS limitation:</b> Apple does not disclose read permission status, so iOS reports every read as
/// <see cref="HealthPermissionStatus.Undisclosed"/> and never as denied. A read the user refused returns no data.
/// </para>
/// </summary>
public class RequestPermissionResult : Result<RequestPermissionError>
{
    /// <summary>
    /// Whether every requested data-type permission is granted. Refused full-history or background access does not
    /// make a request fail: see <see cref="FullHistoryReadStatus"/> and <see cref="BackgroundReadStatus"/>.
    /// On iOS: only reflects write permissions and SDK-level errors, since read permissions are never disclosed.
    /// </summary>
    public override bool IsSuccess => base.IsSuccess && !DeniedPermissions.Any();

    /// <summary>
    /// The status of every requested permission after the request, one entry per read and per write (a permission
    /// requested as read and write is reported as two). Empty when the request failed before the platform could
    /// answer, for example when the health platform is unavailable.
    /// </summary>
    public IList<HealthPermissionStatusResult> PermissionStatuses { get; init; } = [];

    /// <summary>
    /// The requested data-type permissions that are not granted after the request, one entry per read and per write.
    /// </summary>
    public IList<HealthPermissionDto> DeniedPermissions { get; init; } = [];

    /// <summary>
    /// Android only: whether access to data older than 30 days is granted. Null when it was not requested, and on
    /// other platforms.
    /// https://developer.android.com/health-and-fitness/guides/health-connect/plan/data-types#alpha10
    /// </summary>
    public HealthPermissionStatus? FullHistoryReadStatus { get; init; }

    /// <summary>
    /// Android only: whether reading in the background is granted. Null when it was not requested, when the device
    /// does not support it, and on other platforms.
    /// https://developer.android.com/health-and-fitness/guides/health-connect/develop/read-data#background-reads
    /// </summary>
    public HealthPermissionStatus? BackgroundReadStatus { get; init; }

    /// <summary>
    /// Whether the platform showed its permission screen during this request. False when everything requested was
    /// already granted, and also when the platform declined to ask again: Android shows the screen for a permission
    /// at most twice, iOS asks for each type only once. A permission still denied while this is false can only be
    /// granted in the platform's settings.
    /// </summary>
    public bool IsPermissionScreenShown { get; init; }
}
