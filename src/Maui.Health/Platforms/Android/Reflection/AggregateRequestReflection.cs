using Java.Lang.Reflect;
using JClass = Java.Lang.Class;
using static Maui.Health.Platforms.Android.AndroidConstant;

namespace Maui.Health.Platforms.Android.Reflection;

/// <summary>
/// Cached reflection handles for <c>androidx.health.connect.client.request.AggregateRequest</c>
/// — the request used by Health Connect's single-bucket <c>aggregate()</c> API.
/// </summary>
internal static class AggregateRequestReflection
{
    private static JClass? _class;
    private static Constructor? _constructor;

    public static JClass Class
        => _class ??= JClass.ForName(JavaReflection.AggregateRequestClassName)
            ?? throw new InvalidOperationException(
                $"Could not resolve {JavaReflection.AggregateRequestClassName}.");

    /// <summary><c>AggregateRequest(Set&lt;AggregateMetric&gt;, TimeRangeFilter, Set&lt;DataOrigin&gt;)</c></summary>
    public static Constructor Constructor
        => _constructor ??= Class.ResolveAccessibleConstructor(parameterCount: 3);
}
