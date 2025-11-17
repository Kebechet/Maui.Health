using Android.Runtime;
using Java.Util;

namespace Maui.Health.Platforms.Android.Extensions;

internal static class ISetExtensions
{
    internal static IList<T?> ToList<T>(this ISet? javaSet)
        where T : Java.Lang.Object?
    {
        if (javaSet is null)
        {
            return [];
        }

        var listOfStrings = new List<T?>();
        var iterator = javaSet.Iterator();

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
            return new List<string?>();
        }

        var listOfStrings = new List<string?>();
        var iterator = javaSet.Iterator();

        while (iterator.HasNext)
        {
            var element = iterator.Next();
            listOfStrings.Add((string?)element?.JavaCast<Java.Lang.String>());
        }

        return listOfStrings;
    }
}
