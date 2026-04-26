using Android.Runtime;
using AndroidX.Health.Connect.Client.Units;
using Java.Lang.Reflect;
using JClass = Java.Lang.Class;
using Maui.Health.Platforms.Android.Helpers;
using static Maui.Health.Platforms.Android.AndroidConstant;

namespace Maui.Health.Platforms.Android.Reflection.Units;

/// <summary>
/// Cached reflection handles for <c>androidx.health.connect.client.units.Energy</c> — Health
/// Connect's energy unit type. Used on the write path to construct <c>Energy</c> instances
/// via the Kotlin <c>Companion</c> static factory.
/// </summary>
internal static class EnergyReflection
{
    private static JClass? _class;
    private static Java.Lang.Object? _companion;
    private static Method? _kilocalories;

    public static JClass Class
        => _class ??= JavaClassResolver.Resolve(JavaReflection.EnergyClassName)
            ?? throw new InvalidOperationException(
                $"Could not resolve {JavaReflection.EnergyClassName}.");

    /// <summary>The Kotlin <c>Companion</c> singleton that hosts the static factory methods.</summary>
    public static Java.Lang.Object Companion
        => _companion ??= Class.ResolveCompanion();

    /// <summary><c>Energy.kilocalories(double)</c> — the static factory.</summary>
    public static Method Kilocalories
        => _kilocalories ??= Companion.Class!.ResolveAccessibleMethod("kilocalories", Java.Lang.Double.Type!);

    /// <summary>
    /// Constructs an <see cref="Energy"/> from a value in kilocalories via the cached
    /// <see cref="Kilocalories"/> factory.
    /// </summary>
    public static Energy FromKilocalories(double valueInKilocalories)
    {
        var result = Kilocalories.Invoke(Companion, Java.Lang.Double.ValueOf(valueInKilocalories))
            ?? throw new InvalidOperationException(
                $"Energy.kilocalories({valueInKilocalories}) returned null.");
        return Java.Lang.Object.GetObject<Energy>(result.Handle, JniHandleOwnership.DoNotTransfer)!;
    }
}
