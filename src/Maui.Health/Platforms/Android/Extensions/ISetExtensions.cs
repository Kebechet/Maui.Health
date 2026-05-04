using Android.Runtime;
using Java.Util;

namespace Maui.Health.Platforms.Android.Extensions;

internal static class ISetExtensions
{
    internal static IList<T?> ToList<T>(this ISet? javaSet)
        where T : Java.Lang.Object
    {
        if (javaSet is null)
        {
            return [];
        }

        var listOfStrings = new List<T?>();
        // Iterator wrapper holds a JNI global ref; dispose at end of scope.
        // Per-element `element` is intentionally not disposed: JavaCast<T> may return the
        // same managed wrapper instance, in which case disposing would also invalidate the
        // parsedElement we're handing to the caller.
        using var iterator = javaSet.Iterator();

        while (iterator.HasNext)
        {
            var element = iterator.Next();
            var parsedElement = element?.JavaCast<T>();

            listOfStrings.Add(parsedElement);
        }

        return listOfStrings;
    }

    internal static IList<string?> ToList(this ISet? javaSet)
    {
        if (javaSet is null)
        {
            return [];
        }

        var listOfStrings = new List<string?>();
        // Iterator wrapper holds a JNI global ref; dispose at end of scope.
        using var iterator = javaSet.Iterator();

        while (iterator.HasNext)
        {
            // The Java.Lang.String wrapper is converted to a managed string and discarded;
            // dispose it to release the JNI global ref instead of waiting for finalization.
            using var element = iterator.Next();
            listOfStrings.Add((string?)element?.JavaCast<Java.Lang.String>());
        }

        return listOfStrings;
    }
}
