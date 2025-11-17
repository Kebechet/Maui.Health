using Foundation;

namespace Maui.Health.Platforms.iOS.Extensions;

internal static class NSDateExtensions
{
    internal static DateTime ToDateTime(this NSDate nsDate)
    {
        return (DateTime)nsDate;
    }

    // TODO: Is this correct? IOS need Dates in form of UTC. We need convert our -> NSDate with Utc specification.
    internal static NSDate ToNSDate(this DateTimeOffset dto)
    {
        // Force the DateTime into UTC, which NSDate requires
        return (NSDate)dto.UtcDateTime;
    }
}