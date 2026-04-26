using Java.Lang.Reflect;
using JClass = Java.Lang.Class;
using static Maui.Health.Platforms.Android.AndroidConstant;

namespace Maui.Health.Platforms.Android.Reflection;

/// <summary>
/// Cached reflection handles for <c>androidx.health.connect.client.changes.UpsertionChange</c>
/// — one of two implementations of <c>Change</c>, returned inside
/// <see cref="ChangesResponseReflection.GetChanges"/> for inserted or updated records.
/// </summary>
internal static class UpsertionChangeReflection
{
    private static JClass? _class;
    private static Method? _getRecord;

    public static JClass Class
        => _class ??= JClass.ForName(JavaReflection.UpsertionChangeClassName)
            ?? throw new InvalidOperationException(
                $"Could not resolve {JavaReflection.UpsertionChangeClassName}.");

    /// <summary><c>Record getRecord()</c></summary>
    public static Method GetRecord
        => _getRecord ??= Class.ResolveAccessibleMethod("getRecord");
}
