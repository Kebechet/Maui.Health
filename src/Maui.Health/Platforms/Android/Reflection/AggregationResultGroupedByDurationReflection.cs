using Java.Lang.Reflect;
using JClass = Java.Lang.Class;
using Maui.Health.Platforms.Android.Helpers;
using static Maui.Health.Platforms.Android.AndroidConstant;

namespace Maui.Health.Platforms.Android.Reflection;

/// <summary>
/// Cached reflection handles for
/// <c>androidx.health.connect.client.aggregate.AggregationResultGroupedByDuration</c> — one
/// element per bucket returned by <c>aggregateGroupByDuration()</c>.
/// </summary>
internal static class AggregationResultGroupedByDurationReflection
{
    private static JClass? _class;
    private static Method? _getStartTime;
    private static Method? _getEndTime;
    private static Method? _getResult;

    public static JClass Class
        => _class ??= JavaClassResolver.Resolve(JavaReflection.AggregationResultGroupedByDurationClassName)
            ?? throw new InvalidOperationException(
                $"Could not resolve {JavaReflection.AggregationResultGroupedByDurationClassName}.");

    /// <summary><c>java.time.Instant getStartTime()</c></summary>
    public static Method GetStartTime
        => _getStartTime ??= Class.ResolveAccessibleMethod("getStartTime");

    /// <summary><c>java.time.Instant getEndTime()</c></summary>
    public static Method GetEndTime
        => _getEndTime ??= Class.ResolveAccessibleMethod("getEndTime");

    /// <summary><c>AggregationResult getResult()</c></summary>
    public static Method GetResult
        => _getResult ??= Class.ResolveAccessibleMethod("getResult");
}
