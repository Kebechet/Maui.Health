using JClass = Java.Lang.Class;
using static Maui.Health.Platforms.Android.AndroidConstant;

namespace Maui.Health.Platforms.Android.Reflection;

/// <summary>
/// Cached reflection handles for <c>androidx.health.connect.client.aggregate.AggregateMetric</c>.
/// Used as the parameter type for <c>AggregationResult.get(AggregateMetric)</c>.
/// </summary>
internal static class AggregateMetricReflection
{
    private static JClass? _class;

    public static JClass Class
        => _class ??= JClass.ForName(JavaReflection.AggregateMetricClassName)
            ?? throw new InvalidOperationException(
                $"Could not resolve {JavaReflection.AggregateMetricClassName}.");
}
