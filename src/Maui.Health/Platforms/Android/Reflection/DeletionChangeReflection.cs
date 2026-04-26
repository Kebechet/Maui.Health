using Java.Lang.Reflect;
using JClass = Java.Lang.Class;
using static Maui.Health.Platforms.Android.AndroidConstant;

namespace Maui.Health.Platforms.Android.Reflection;

/// <summary>
/// Cached reflection handles for <c>androidx.health.connect.client.changes.DeletionChange</c>
/// — one of two implementations of <c>Change</c>, returned inside
/// <see cref="ChangesResponseReflection.GetChanges"/> for removed records.
/// </summary>
internal static class DeletionChangeReflection
{
    private static JClass? _class;
    private static Method? _getDeletedRecordId;

    public static JClass Class
        => _class ??= JClass.ForName(JavaReflection.DeletionChangeClassName)
            ?? throw new InvalidOperationException(
                $"Could not resolve {JavaReflection.DeletionChangeClassName}.");

    /// <summary><c>String getDeletedRecordId()</c> (with <c>getRecordId</c> name fallback).</summary>
    public static Method GetDeletedRecordId
        => _getDeletedRecordId ??= Class.ResolveAccessibleMethodOneOf("getDeletedRecordId", "getRecordId");
}
