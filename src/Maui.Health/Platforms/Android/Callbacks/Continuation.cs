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
        //Check if there are any exception. We don't have access to the class Kotlin.Result.Failure. But we can extraxt the exception (Throwable) from the field in the class.
        var exceptionField = result.Class.GetDeclaredFields().FirstOrDefault(x => x.Name.Contains("exception", StringComparison.OrdinalIgnoreCase));
        if (exceptionField != null)
        {
            var exception = exceptionField.Get(result).JavaCast<Throwable>();
            _taskCompletionSource.TrySetException(new System.Exception(exception?.Message));
            return;
        }
        else
        {
            _taskCompletionSource.TrySetResult(result);
        }
    }
}