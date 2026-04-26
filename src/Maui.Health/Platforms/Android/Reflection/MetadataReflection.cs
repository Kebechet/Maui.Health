using Java.Lang.Reflect;
using JClass = Java.Lang.Class;
using static Maui.Health.Platforms.Android.AndroidConstant;

namespace Maui.Health.Platforms.Android.Reflection;

/// <summary>
/// Cached reflection handles for
/// <c>androidx.health.connect.client.records.metadata.Metadata</c> — the per-record metadata
/// object returned by <see cref="RecordReflection.GetMetadata"/>.
/// </summary>
internal static class MetadataReflection
{
    private static JClass? _class;
    private static Method? _getId;

    public static JClass Class
        => _class ??= JClass.ForName(JavaReflection.MetadataClassName)
            ?? throw new InvalidOperationException(
                $"Could not resolve {JavaReflection.MetadataClassName}.");

    /// <summary><c>String getId()</c></summary>
    public static Method GetId
        => _getId ??= Class.ResolveAccessibleMethod("getId");
}
