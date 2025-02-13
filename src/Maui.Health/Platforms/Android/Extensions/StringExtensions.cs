using System.Text.RegularExpressions;

namespace Maui.Health.Platforms.Android.Extensions;

internal static class StringExtensions
{
    //SCREAMING_SNAKE_CASE  
    internal static string ToScreamingSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return Regex.Replace(input, "(?<!^)([A-Z])", "_$1").ToUpper();
    }
}
