using Maui.Health.Constants;

namespace Maui.Health.Platforms.Android.Extensions;

public static class InstantExtensions
{
    public static DateTimeOffset ToDateTimeOffset(this Java.Time.Instant instant)
    {
#pragma warning disable CA1416
        var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(instant.ToEpochMilli());
#pragma warning restore CA1416
        return dateTime;
    }

    public static Java.Time.Instant ToJavaInstant(this DateTimeOffset dateTimeOffset)
    {
#pragma warning disable CA1416
        return Java.Time.Instant.Parse(dateTimeOffset.ToUniversalTime().ToString(DataFormats.Iso8601Utc))!;
#pragma warning restore CA1416
    }
}
