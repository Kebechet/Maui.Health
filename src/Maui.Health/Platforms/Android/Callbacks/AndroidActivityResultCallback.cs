using Android.Runtime;
using AndroidX.Activity.Result;

namespace Maui.Health.Platforms.Android.Callbacks;

internal sealed class AndroidActivityResultCallback<TResult> : ResultOnlyCallbackBase<TResult>, IActivityResultCallback
    where TResult : IJavaObject?
{
    public AndroidActivityResultCallback(CancellationToken cancellationToken) : base(cancellationToken)
    { }

    public void OnActivityResult(Java.Lang.Object? result)
    {
        var iResult = (IJavaObject?)result;
        if (result is null)
        {
            ReportSuccess(default);
            return;
        }

        var parsedResult = (TResult)(IJavaObject)result;

        ReportSuccess(parsedResult);
    }
}
