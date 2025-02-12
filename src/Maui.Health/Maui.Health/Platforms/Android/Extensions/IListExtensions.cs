namespace Maui.Health.Platforms.Android.Extensions;

internal static class IListExtensions
{
    internal static IList<string?> ToList(this IList<Java.Lang.String?> list)
    {
        var listOfStrings = new List<string?>();
        foreach (var item in list)
        {
            var castedItem = (string?)item;
            listOfStrings.Add(castedItem);
        }

        return listOfStrings;
    }
}
