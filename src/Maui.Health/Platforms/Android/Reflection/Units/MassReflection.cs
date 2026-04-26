using Android.Runtime;
using AndroidX.Health.Connect.Client.Units;
using Java.Lang.Reflect;
using JClass = Java.Lang.Class;
using static Maui.Health.Platforms.Android.AndroidConstant;

namespace Maui.Health.Platforms.Android.Reflection.Units;

/// <summary>
/// Cached reflection handles for <c>androidx.health.connect.client.units.Mass</c> — Health
/// Connect's mass unit type. Used on the write path to construct <c>Mass</c> instances via
/// the Kotlin <c>Companion</c> static factory.
/// </summary>
internal static class MassReflection
{
    private static JClass? _class;
    private static Java.Lang.Object? _companion;
    private static Method? _kilograms;

    public static JClass Class
        => _class ??= JClass.ForName(JavaReflection.MassClassName)
            ?? throw new InvalidOperationException(
                $"Could not resolve {JavaReflection.MassClassName}.");

    /// <summary>The Kotlin <c>Companion</c> singleton that hosts the static factory methods.</summary>
    public static Java.Lang.Object Companion
        => _companion ??= Class.ResolveCompanion();

    /// <summary><c>Mass.kilograms(double)</c> — the static factory.</summary>
    public static Method Kilograms
        => _kilograms ??= Companion.Class!.ResolveAccessibleMethod("kilograms", Java.Lang.Double.Type!);

    /// <summary>
    /// Constructs a <see cref="Mass"/> from a value in kilograms via the cached
    /// <see cref="Kilograms"/> factory. Throws if the factory call fails (it shouldn't, given
    /// the resolution succeeded earlier).
    /// </summary>
    public static Mass FromKilograms(double valueInKilograms)
    {
        var result = Kilograms.Invoke(Companion, Java.Lang.Double.ValueOf(valueInKilograms))
            ?? throw new InvalidOperationException(
                $"Mass.kilograms({valueInKilograms}) returned null.");
        return Java.Lang.Object.GetObject<Mass>(result.Handle, JniHandleOwnership.DoNotTransfer)!;
    }
}
