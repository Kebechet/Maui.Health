namespace Maui.Health.Models;

public class Result<TError>
    where TError : struct
{
    public virtual bool IsSuccess => Error is null && ErrorException is null;
    public bool IsError => !IsSuccess;

    public TError? Error { get; init; } = null;
    public Exception? ErrorException { get; init; } = null;
}
