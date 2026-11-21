using Maui.Health.Enums.Errors;

namespace Maui.Health.Models;

/// <summary>
/// Result of <c>IHealthService.GetChanges</c>.
/// </summary>
/// <remarks>
/// <para>Success with <see cref="Changes"/> non-null means the platform returned a changes
/// payload. Success with <see cref="Changes"/> null means the token was invalid or expired
/// (tokens expire after 30 days) and the caller should re-issue a token via
/// <c>IHealthService.GetChangesToken</c>.</para>
///
/// <para>Failure (<see cref="Result.IsError"/> true) means the platform call itself failed;
/// check <see cref="Result.ErrorException"/>. A read refused because the app is in the
/// background sets <see cref="Result{TError}.Error"/> to
/// <see cref="ReadHealthDataError.BackgroundReadNotPermitted"/>.</para>
/// </remarks>
public class ChangesReadResult : Result<ReadHealthDataError>
{
    /// <summary>
    /// The changes payload, or null when the token was invalid/expired. Always null on
    /// failure.
    /// </summary>
    public HealthChangesResult? Changes { get; init; }
}
