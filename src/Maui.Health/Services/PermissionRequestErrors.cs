using Kebechet.Types.Health.Enums.Errors;
using Kebechet.Types.Health.Models;

namespace Maui.Health.Services;

/// <summary>
/// How a read or a write reports the permission request it made first, when that request did not grant it. Only a
/// refusal is a denied permission: a cancelled request is a cancellation, whatever else went wrong is not the user's
/// answer. Touches no platform API, so the tests link it as source.
/// </summary>
internal static class PermissionRequestErrors
{
    internal static TResult ToReadResult<TResult>(RequestPermissionResult requestPermissionResult)
        where TResult : Result<ReadHealthDataError>, new()
    {
        ReadHealthDataError? readError = requestPermissionResult.Error switch
        {
            RequestPermissionError.MissingPermissions => ReadHealthDataError.PermissionDenied,
            RequestPermissionError.Cancelled => ReadHealthDataError.Cancelled,
            _ => null,
        };

        return new TResult
        {
            Error = readError,
            ErrorException = requestPermissionResult.ErrorException ?? (readError is null
                ? new InvalidOperationException($"Permission request failed: {requestPermissionResult.Error}")
                : null),
        };
    }

    internal static WriteHealthDataError ToWriteError(RequestPermissionError? requestPermissionError)
    {
        return requestPermissionError switch
        {
            RequestPermissionError.MissingPermissions => WriteHealthDataError.PermissionDenied,
            RequestPermissionError.Cancelled => WriteHealthDataError.Cancelled,
            RequestPermissionError.SdkUnavailable or RequestPermissionError.SdkUnavailableProviderUpdateRequired => WriteHealthDataError.SdkUnavailable,
            _ => WriteHealthDataError.UnexpectedException,
        };
    }

    internal static UpdateHealthDataError ToUpdateError(RequestPermissionError? requestPermissionError)
    {
        return requestPermissionError switch
        {
            RequestPermissionError.MissingPermissions => UpdateHealthDataError.PermissionDenied,
            RequestPermissionError.Cancelled => UpdateHealthDataError.Cancelled,
            RequestPermissionError.SdkUnavailable or RequestPermissionError.SdkUnavailableProviderUpdateRequired => UpdateHealthDataError.SdkUnavailable,
            _ => UpdateHealthDataError.UnexpectedException,
        };
    }
}
