using Android.Runtime;

namespace Maui.Health.Platforms.Android.Callbacks;

internal abstract class ResultOnlyCallbackBase<TResult> : Java.Lang.Object
    where TResult : IJavaObject?
{
    private readonly TaskCompletionSource<TResult?> _taskCompletionSource;

    public ResultOnlyCallbackBase(CancellationToken cancellationToken)
    {
        _taskCompletionSource = new TaskCompletionSource<TResult?>();
        cancellationToken.Register(() => _taskCompletionSource.TrySetCanceled());
    }

    public Task<TResult?> Task => _taskCompletionSource.Task;

    protected void ReportSuccess(TResult? result)
    {
        _taskCompletionSource.TrySetResult(result);
    }
}