using Android.Runtime;
using Java.Lang;
using Kotlin.Coroutines;

namespace Maui.Health.Platforms.Android.Callbacks;

internal class Continuation : Java.Lang.Object, IContinuation
{
    public ICoroutineContext Context => EmptyCoroutineContext.Instance;

    private readonly TaskCompletionSource<Java.Lang.Object> _taskCompletionSource;

    public Continuation(TaskCompletionSource<Java.Lang.Object> taskCompletionSource, CancellationToken cancellationToken)
    {
        _taskCompletionSource = taskCompletionSource;
        cancellationToken.Register(() => _taskCompletionSource.TrySetCanceled());
    }

    public void ResumeWith(Java.Lang.Object result)
    {
        try
        {
            if (result is null)
            {
                _taskCompletionSource.TrySetResult(null!);
                return;
            }

            // Check if there are any exception. We don't have access to the class Kotlin.Result.Failure.
            // But we can extract the exception (Throwable) from the field in the class.
            var exceptionField = result.Class?.GetDeclaredFields()?.FirstOrDefault(x => x.Name != null && x.Name.Contains("exception", StringComparison.OrdinalIgnoreCase));
            if (exceptionField is null)
            {
                _taskCompletionSource.TrySetResult(result);
                return;
            }

            exceptionField.Accessible = true;
            var exception = exceptionField.Get(result)?.JavaCast<Throwable>();

            if (exception is not null)
            {
                _taskCompletionSource.TrySetException(new System.Exception(exception.Message ?? "Unknown error"));
                return;
            }

            _taskCompletionSource.TrySetResult(result);
        }
        catch (System.Exception)
        {
            // If we can't process the result properly, just complete successfully
            // This prevents crashes when Health Connect returns unexpected result formats
            _taskCompletionSource.TrySetResult(result ?? null!);
        }
    }
}