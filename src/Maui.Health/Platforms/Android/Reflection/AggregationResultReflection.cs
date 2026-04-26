using Java.Lang.Reflect;
using JClass = Java.Lang.Class;
using Maui.Health.Platforms.Android.Helpers;
using static Maui.Health.Platforms.Android.AndroidConstant;

namespace Maui.Health.Platforms.Android.Reflection;

/// <summary>
/// Cached reflection handles for <c>androidx.health.connect.client.aggregate.AggregationResult</c>.
/// </summary>
internal static class AggregationResultReflection
{
    private static JClass? _class;
    private static Method? _get;
    private static Method? _getDataOrigins;

    public static JClass Class
        => _class ??= JavaClassResolver.Resolve(JavaReflection.AggregationResultClassName)
            ?? throw new InvalidOperationException(
                $"Could not resolve {JavaReflection.AggregationResultClassName}.");

    /// <summary><c>Object get(AggregateMetric&lt;?&gt; metric)</c></summary>
    public static Method Get
        => _get ??= Class.ResolveAccessibleMethod("get", AggregateMetricReflection.Class);

    /// <summary><c>Set&lt;DataOrigin&gt; getDataOrigins()</c></summary>
    public static Method GetDataOrigins
        => _getDataOrigins ??= Class.ResolveAccessibleMethod("getDataOrigins");
}
