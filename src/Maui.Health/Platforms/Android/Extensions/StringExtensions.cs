using System.Text.RegularExpressions;

namespace Maui.Health.Platforms.Android.Extensions;

internal static class StringExtensions
{
    private const string NumberExtractionPattern = @"(\d+\.?\d*)";

    //SCREAMING_SNAKE_CASE
    internal static string ToScreamingSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return Regex.Replace(input, "(?<!^)([A-Z])", "_$1").ToUpper();
    }

    /// <summary>
    /// Tries to parse a double value from a string using regex.
    /// </summary>
    public static bool TryParseFromString(this string? stringValue, out double value)
    {
        value = 0;

        if (string.IsNullOrEmpty(stringValue))
        {
            return false;
        }

        var match = Regex.Match(stringValue, NumberExtractionPattern);

        if (!match.Success)
        {
            return false;
        }

        return double.TryParse(match.Groups[1].Value, out value);
    }
}
