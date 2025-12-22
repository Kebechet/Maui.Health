using Foundation;

namespace Maui.Health.Platforms.iOS.Extensions;

internal static class NSDateExtensions
{
    internal static DateTime ToDateTime(this NSDate nsDate)
    {
        // NSDate represents UTC time, but (DateTime)nsDate casts to local time
        // We need to ensure we return UTC time
        var localDateTime = (DateTime)nsDate;
        return DateTime.SpecifyKind(localDateTime.ToUniversalTime(), DateTimeKind.Utc);
    }

    internal static NSDate ToNSDate(this DateTimeOffset dto)
    {
        // Force the DateTime into UTC, which NSDate requires
        return (NSDate)dto.UtcDateTime;
    }

    internal static DateTimeOffset ToDateTimeOffset(this NSDate nsDate)
    {
        // NSDate is always UTC - convert directly to DateTimeOffset in UTC
        var utcDateTime = nsDate.ToDateTime();
        return new DateTimeOffset(utcDateTime, TimeSpan.Zero);
    }
}