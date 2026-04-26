using Java.Lang.Reflect;
using JClass = Java.Lang.Class;
using Maui.Health.Platforms.Android.Helpers;
using static Maui.Health.Platforms.Android.AndroidConstant;

namespace Maui.Health.Platforms.Android.Reflection;

/// <summary>
/// Cached reflection handles for
/// <c>androidx.health.connect.client.response.ChangesResponse</c> — the result of
/// <c>HealthConnectClient.getChanges(token)</c>.
/// </summary>
internal static class ChangesResponseReflection
{
    private static JClass? _class;
    private static Method? _getChanges;
    private static Method? _getNextChangesToken;
    private static Method? _getHasMore;

    public static JClass Class
        => _class ??= JavaClassResolver.Resolve(JavaReflection.ChangesResponseClassName)
            ?? throw new InvalidOperationException(
                $"Could not resolve {JavaReflection.ChangesResponseClassName}.");

    /// <summary><c>List&lt;Change&gt; getChanges()</c></summary>
    public static Method GetChanges
        => _getChanges ??= Class.ResolveAccessibleMethod("getChanges");

    /// <summary><c>String getNextChangesToken()</c></summary>
    public static Method GetNextChangesToken
        => _getNextChangesToken ??= Class.ResolveAccessibleMethod("getNextChangesToken");

    /// <summary><c>boolean getHasMore()</c> (with <c>hasMore</c> name fallback).</summary>
    public static Method GetHasMore
        => _getHasMore ??= Class.ResolveAccessibleMethodOneOf("getHasMore", "hasMore");
}
