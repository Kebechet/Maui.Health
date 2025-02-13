using Android.Runtime;
using AndroidX.Health.Connect.Client.Aggregate;
using Kotlin.Coroutines;
using Maui.Health.Platforms.Android.Extensions;

namespace Maui.Health.Platforms.Android.Callbacks;

internal static class KotlinResolver
{
    internal static async Task<TResult?> Process<TResult, T1>(Func<T1, IContinuation, Java.Lang.Object?> method, T1 parameter1)
        where TResult : Java.Lang.Object
    {
        var taskCompletionSource = new TaskCompletionSource<Java.Lang.Object>();

        var result = method(parameter1, new Continuation(taskCompletionSource, default));

        return await ParseResult<TResult>(result, taskCompletionSource);
    }

    internal static async Task<IList<TResult?>?> ProcessList<TResult>(Func<IContinuation, Java.Lang.Object?> method)
        where TResult : Java.Lang.Object
    {
        var taskCompletionSource = new TaskCompletionSource<Java.Lang.Object>();

        var result = method(new Continuation(taskCompletionSource, default));

        return await ParseListResult<TResult>(result, taskCompletionSource);
    }

    internal static async Task<IList<TResult?>?> ProcessList<TResult, T1>(Func<T1, IContinuation, Java.Lang.Object> method, T1 parameter1)
        where TResult : Java.Lang.Object
    {
        var taskCompletionSource = new TaskCompletionSource<Java.Lang.Object>();

        Java.Lang.Object result = method(parameter1, new Continuation(taskCompletionSource, default));

        return await ParseListResult<TResult>(result, taskCompletionSource);
    }

    private static async Task<TResult?> ParseResult<TResult>(Java.Lang.Object? result, TaskCompletionSource<Java.Lang.Object> taskCompletionSource)
        where TResult : Java.Lang.Object
    {
        if (result is Java.Lang.Enum javaEnum)
        {
            var currentState = Enum.Parse<CoroutineState>(javaEnum.ToString());
            if (currentState == CoroutineState.COROUTINE_SUSPENDED)
            {
                result = await taskCompletionSource.Task;
            }
        }

        return result is TResult tResult
            ? tResult
            : null;
    }
    private static async Task<IList<TResult?>?> ParseListResult<TResult>(Java.Lang.Object? result, TaskCompletionSource<Java.Lang.Object> taskCompletionSource)
        where TResult : Java.Lang.Object
    {
        if (result is Java.Lang.Enum CoroutineSingletons)
        {
            var currentState = Enum.Parse<CoroutineState>(CoroutineSingletons.ToString());
            if (currentState == CoroutineState.COROUTINE_SUSPENDED)
            {
                result = await taskCompletionSource.Task;
            }
        }

        if (result is JavaList javaList)
        {
            var aggregationResults = new List<AggregationResultGroupedByDuration>();
            for (int i = 0; i < javaList.Size(); i++)
            {
                if (javaList.Get(i) is AggregationResultGroupedByDuration item)
                {
                    aggregationResults.Add(item);
                }
            }
            return (IList<TResult?>)aggregationResults;
        }

        if (result is Kotlin.Collections.AbstractMutableSet abstractMutableSet)
        {
            var javaSet = abstractMutableSet.JavaCast<Java.Util.ISet>();
            return javaSet?.ToList<TResult?>();
        }

        return null;
    }
}
