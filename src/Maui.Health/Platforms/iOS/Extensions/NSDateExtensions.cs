using Foundation;

namespace Maui.Health.Platforms.iOS.Extensions;

internal static class NSDateExtensions
{
    internal static DateTime ToDateTime(this NSDate nsDate)
    {
        var reference = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return reference.AddSeconds(nsDate.SecondsSinceReferenceDate);
    }
}