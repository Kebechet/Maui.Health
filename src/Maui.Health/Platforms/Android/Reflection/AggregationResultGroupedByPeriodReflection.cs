using Java.Lang.Reflect;
using JClass = Java.Lang.Class;
using Maui.Health.Platforms.Android.Helpers;
using static Maui.Health.Platforms.Android.AndroidConstant;

namespace Maui.Health.Platforms.Android.Reflection;

/// <summary>
/// Cached reflection handles for
/// <c>androidx.health.connect.client.aggregate.AggregationResultGroupedByPeriod</c> — one
/// element per calendar-day bucket returned by <c>aggregateGroupByPeriod()</c>. The start
/// and end times come back as <see cref="Java.Time.LocalDateTime"/> (no offset / no zone),
/// because Period semantics are calendar-relative; converting back to
/// <see cref="System.DateTimeOffset"/> requires applying the recording zone's offset at the
/// bucket boundary moment.
/// </summary>
internal static class AggregationResultGroupedByPeriodReflection
{
    private static JClass? _class;
    private static Method? _getStartTime;
    private static Method? _getEndTime;
    private static Method? _getResult;

    public static JClass Class
        => _class ??= JavaClassResolver.Resolve(JavaReflection.AggregationResultGroupedByPeriodClassName)
            ?? throw new InvalidOperationException(
                $"Could not resolve {JavaReflection.AggregationResultGroupedByPeriodClassName}.");

    /// <summary><c>java.time.LocalDateTime getStartTime()</c></summary>
    public static Method GetStartTime
        => _getStartTime ??= Class.ResolveAccessibleMethod("getStartTime");

    /// <summary><c>java.time.LocalDateTime getEndTime()</c></summary>
    public static Method GetEndTime
        => _getEndTime ??= Class.ResolveAccessibleMethod("getEndTime");

    /// <summary><c>AggregationResult getResult()</c></summary>
    public static Method GetResult
        => _getResult ??= Class.ResolveAccessibleMethod("getResult");
}
