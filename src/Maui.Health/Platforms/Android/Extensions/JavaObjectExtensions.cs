using Android.Runtime;

namespace Maui.Health.Platforms.Android.Extensions;

internal static class JavaObjectExtensions
{
    internal static bool TryConvertToDouble(this Java.Lang.Object? obj, out double value)
    {
        value = 0;

        if (obj is null)
        {
            return false;
        }

        // Use Java.Lang.Number base class for unified conversion
        if (obj is Java.Lang.Number javaNumber)
        {
            value = javaNumber.DoubleValue();
            return true;
        }

        return false;
    }

    /// <summary>
    /// The Health Connect app (Android 13 and older) answers with Kotlin collection classes such as
    /// <c>kotlin.collections.EmptySet</c> that have no managed binding: they reach us as a plain
    /// <see cref="Java.Lang.Object"/>, so <c>javaObject is Java.Util.ISet</c> is false although the Java object implements
    /// it. Asking the Java side finds them. Returns null when the object does not implement <typeparamref name="TInterface"/>.
    /// </summary>
    internal static TInterface? AsJavaInterface<TInterface>(this Java.Lang.Object? javaObject)
        where TInterface : class, IJavaObject
    {
        if (javaObject is TInterface managedInterface)
        {
            return managedInterface;
        }

        if (javaObject is null)
        {
            return null;
        }

        using var interfaceClass = Java.Lang.Class.FromType(typeof(TInterface));
        if (!interfaceClass.IsInstance(javaObject))
        {
            return null;
        }

        return javaObject.JavaCast<TInterface>();
    }
}
