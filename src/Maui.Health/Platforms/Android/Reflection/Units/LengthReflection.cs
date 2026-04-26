using Android.Runtime;
using AndroidX.Health.Connect.Client.Units;
using Java.Lang.Reflect;
using JClass = Java.Lang.Class;
using Maui.Health.Platforms.Android.Helpers;
using static Maui.Health.Platforms.Android.AndroidConstant;

namespace Maui.Health.Platforms.Android.Reflection.Units;

/// <summary>
/// Cached reflection handles for <c>androidx.health.connect.client.units.Length</c> — Health
/// Connect's length unit type. Used on the write path to construct <c>Length</c> instances
/// via the Kotlin <c>Companion</c> static factory.
/// </summary>
internal static class LengthReflection
{
    private static JClass? _class;
    private static Java.Lang.Object? _companion;
    private static Method? _meters;

    public static JClass Class
        => _class ??= JavaClassResolver.Resolve(JavaReflection.LengthClassName)
            ?? throw new InvalidOperationException(
                $"Could not resolve {JavaReflection.LengthClassName}.");

    /// <summary>The Kotlin <c>Companion</c> singleton that hosts the static factory methods.</summary>
    public static Java.Lang.Object Companion
        => _companion ??= Class.ResolveCompanion();

    /// <summary><c>Length.meters(double)</c> — the static factory.</summary>
    public static Method Meters
        => _meters ??= Companion.Class!.ResolveAccessibleMethod("meters", Java.Lang.Double.Type!);

    /// <summary>
    /// Constructs a <see cref="Length"/> from a value in meters via the cached
    /// <see cref="Meters"/> factory.
    /// </summary>
    public static Length FromMeters(double valueInMeters)
    {
        var result = Meters.Invoke(Companion, Java.Lang.Double.ValueOf(valueInMeters))
            ?? throw new InvalidOperationException(
                $"Length.meters({valueInMeters}) returned null.");
        return Java.Lang.Object.GetObject<Length>(result.Handle, JniHandleOwnership.DoNotTransfer)!;
    }
}
