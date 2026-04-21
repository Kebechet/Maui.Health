namespace Maui.Health.Models;

/// <summary>
/// Result of <see cref="Services.IHealthService.GetChanges(string, System.Threading.CancellationToken)"/>.
/// </summary>
/// <remarks>
/// <para>Success with <see cref="Changes"/> non-null means the platform returned a changes
/// payload. Success with <see cref="Changes"/> null means the token was invalid or expired
/// (tokens expire after 30 days) and the caller should re-issue a token via
/// <see cref="Services.IHealthService.GetChangesToken(System.Collections.Generic.IList{Enums.HealthDataType}, System.Threading.CancellationToken)"/>.</para>
///
/// <para>Failure (<see cref="Result.IsError"/> true) means the platform call itself failed;
/// check <see cref="Result.ErrorException"/>.</para>
/// </remarks>
public class ChangesReadResult : Result
{
    /// <summary>
    /// The changes payload, or null when the token was invalid/expired. Always null on
    /// failure.
    /// </summary>
    public HealthChangesResult? Changes { get; init; }
}
