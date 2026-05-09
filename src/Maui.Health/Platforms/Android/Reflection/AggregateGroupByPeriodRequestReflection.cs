using Java.Lang.Reflect;
using JClass = Java.Lang.Class;
using Maui.Health.Platforms.Android.Helpers;
using static Maui.Health.Platforms.Android.AndroidConstant;

namespace Maui.Health.Platforms.Android.Reflection;

/// <summary>
/// Cached reflection handles for
/// <c>androidx.health.connect.client.request.AggregateGroupByPeriodRequest</c> — the request
/// used by Health Connect's <c>aggregateGroupByPeriod()</c> API.
/// </summary>
internal static class AggregateGroupByPeriodRequestReflection
{
    private static JClass? _class;
    private static Constructor? _constructor;

    public static JClass Class
        => _class ??= JavaClassResolver.Resolve(JavaReflection.AggregateGroupByPeriodRequestClassName)
            ?? throw new InvalidOperationException(
                $"Could not resolve {JavaReflection.AggregateGroupByPeriodRequestClassName}.");

    /// <summary><c>AggregateGroupByPeriodRequest(Set&lt;AggregateMetric&gt;, TimeRangeFilter, Period, Set&lt;DataOrigin&gt;)</c></summary>
    public static Constructor Constructor
        => _constructor ??= Class.ResolveAccessibleConstructor(parameterCount: 4);
}
