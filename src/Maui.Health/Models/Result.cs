namespace Maui.Health.Models;

/// <summary>
/// Generic result type that indicates success or carries a typed error and/or exception.
/// </summary>
/// <typeparam name="TError">The enum type representing possible errors.</typeparam>
public class Result<TError>
    where TError : struct
{
    /// <summary>
    /// Whether the operation succeeded (no error and no exception).
    /// </summary>
    public virtual bool IsSuccess => Error is null && ErrorException is null;
    /// <summary>
    /// Whether the operation failed.
    /// </summary>
    public bool IsError => !IsSuccess;

    /// <summary>
    /// The typed error value, if any.
    /// </summary>
    public TError? Error { get; init; } = null;
    /// <summary>
    /// The exception that occurred, if any.
    /// </summary>
    public Exception? ErrorException { get; init; } = null;
}
