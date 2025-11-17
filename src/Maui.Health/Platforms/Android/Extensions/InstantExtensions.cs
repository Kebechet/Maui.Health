namespace Maui.Health.Platforms.Android.Extensions;

internal static class InstantExtensions
{
    internal static DateTimeOffset ToDateTimeOffset(this Java.Time.Instant instant)
    {
#pragma warning disable CA1416 // Validate platform compatibility
        var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(instant.ToEpochMilli());
#pragma warning restore CA1416 // Validate platform compatibility
        return dateTime;
    }
}
