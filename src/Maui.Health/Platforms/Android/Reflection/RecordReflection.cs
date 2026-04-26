using Java.Lang.Reflect;
using JClass = Java.Lang.Class;
using static Maui.Health.Platforms.Android.AndroidConstant;

namespace Maui.Health.Platforms.Android.Reflection;

/// <summary>
/// Cached reflection handles for <c>androidx.health.connect.client.records.Record</c> — the
/// abstract base interface for every concrete record type (StepsRecord, WeightRecord, etc.).
/// </summary>
/// <remarks>
/// The <see cref="GetMetadata"/> handle is resolved once on the abstract <c>Record</c>
/// interface; <see cref="Method.Invoke(Java.Lang.Object?, Java.Lang.Object[])"/> dispatches
/// dynamically to the concrete subclass at call time, so the same cached handle works for any
/// record type encountered during a sync.
/// </remarks>
internal static class RecordReflection
{
    private static JClass? _class;
    private static Method? _getMetadata;

    public static JClass Class
        => _class ??= JClass.ForName(JavaReflection.RecordClassName)
            ?? throw new InvalidOperationException(
                $"Could not resolve {JavaReflection.RecordClassName}.");

    /// <summary><c>Metadata getMetadata()</c></summary>
    public static Method GetMetadata
        => _getMetadata ??= Class.ResolveAccessibleMethod("getMetadata");
}
