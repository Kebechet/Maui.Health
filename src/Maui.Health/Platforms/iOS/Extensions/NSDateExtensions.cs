using Foundation;

namespace Maui.Health.Platforms.iOS.Extensions;

internal static class NSDateExtensions
{
    internal static DateTime ToDateTime(this NSDate nsDate)
    {
        return (DateTime)nsDate;
    }
}