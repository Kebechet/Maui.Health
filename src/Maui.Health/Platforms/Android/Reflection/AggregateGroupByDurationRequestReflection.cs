using Java.Lang.Reflect;
using JClass = Java.Lang.Class;
using static Maui.Health.Platforms.Android.AndroidConstant;

namespace Maui.Health.Platforms.Android.Reflection;

/// <summary>
/// Cached reflection handles for
/// <c>androidx.health.connect.client.request.AggregateGroupByDurationRequest</c> — the request
/// used by Health Connect's <c>aggregateGroupByDuration()</c> API.
/// </summary>
internal static class AggregateGroupByDurationRequestReflection
{
    private static JClass? _class;
    private static Constructor? _constructor;

    public static JClass Class
        => _class ??= JClass.ForName(JavaReflection.AggregateGroupByDurationRequestClassName)
            ?? throw new InvalidOperationException(
                $"Could not resolve {JavaReflection.AggregateGroupByDurationRequestClassName}.");

    /// <summary><c>AggregateGroupByDurationRequest(Set&lt;AggregateMetric&gt;, TimeRangeFilter, Duration, Set&lt;DataOrigin&gt;)</c></summary>
    public static Constructor Constructor
        => _constructor ??= Class.ResolveAccessibleConstructor(parameterCount: 4);
}
