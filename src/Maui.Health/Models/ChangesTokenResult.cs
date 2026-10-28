using Maui.Health.Enums;
using Maui.Health.Enums.Errors;

namespace Maui.Health.Models;

/// <summary>
/// Result of <see cref="Services.IHealthService.GetChangesToken(System.Collections.Generic.IList{HealthDataType}, System.Threading.CancellationToken)"/>.
/// </summary>
/// <remarks>
/// On success, <see cref="Token"/> is the opaque token the caller must persist and use on
/// the next <see cref="Services.IHealthService.GetChanges(string, System.Threading.CancellationToken)"/>
/// call. On failure, <see cref="Result.IsError"/> is <c>true</c> and <see cref="Token"/> is
/// null. Typical failures: permission denial, SDK unavailable, platform query error. A call
/// refused because the app is in the background sets <see cref="Result{TError}.Error"/> to
/// <see cref="ReadHealthDataError.BackgroundReadNotPermitted"/>.
/// </remarks>
public class ChangesTokenResult : Result<ReadHealthDataError>
{
    /// <summary>
    /// The opaque changes token assigned by the platform. Null on failure.
    /// </summary>
    public string? Token { get; init; }
}
