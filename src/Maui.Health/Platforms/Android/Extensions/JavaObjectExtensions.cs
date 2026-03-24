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
}
