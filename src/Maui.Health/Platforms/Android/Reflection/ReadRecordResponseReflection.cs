using Java.Lang.Reflect;
using JClass = Java.Lang.Class;
using static Maui.Health.Platforms.Android.AndroidConstant;

namespace Maui.Health.Platforms.Android.Reflection;

/// <summary>
/// Cached reflection handles for
/// <c>androidx.health.connect.client.response.ReadRecordResponse</c> — the result of
/// <c>HealthConnectClient.readRecord(class, id)</c>.
/// </summary>
internal static class ReadRecordResponseReflection
{
    private static JClass? _class;
    private static Method? _getRecord;

    public static JClass Class
        => _class ??= JClass.ForName(JavaReflection.ReadRecordResponseClassName)
            ?? throw new InvalidOperationException(
                $"Could not resolve {JavaReflection.ReadRecordResponseClassName}.");

    /// <summary><c>R getRecord()</c> — dynamically dispatched to the requested record subtype.</summary>
    public static Method GetRecord
        => _getRecord ??= Class.ResolveAccessibleMethod("getRecord");
}
