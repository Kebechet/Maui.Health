using Java.Lang.Reflect;
using JClass = Java.Lang.Class;
using Maui.Health.Platforms.Android.Helpers;
using static Maui.Health.Platforms.Android.AndroidConstant;

namespace Maui.Health.Platforms.Android.Reflection;

/// <summary>
/// Cached reflection handles for
/// <c>androidx.health.connect.client.records.metadata.DataOrigin</c> — exposed by
/// <c>AggregationResult.getDataOrigins()</c> as the apps that contributed to a bucket.
/// </summary>
internal static class DataOriginReflection
{
    private static JClass? _class;
    private static Method? _getPackageName;

    public static JClass Class
        => _class ??= JavaClassResolver.Resolve(JavaReflection.DataOriginClassName)
            ?? throw new InvalidOperationException(
                $"Could not resolve {JavaReflection.DataOriginClassName}.");

    /// <summary><c>String getPackageName()</c></summary>
    public static Method GetPackageName
        => _getPackageName ??= Class.ResolveAccessibleMethod("getPackageName");
}
