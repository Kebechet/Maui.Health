using Maui.Health.Enums.Errors;

namespace Maui.Health.Models;

public class SdkResult
{
    public bool IsSuccess => Error is null;

    public SdkCheckError? Error { get; init; }
}
