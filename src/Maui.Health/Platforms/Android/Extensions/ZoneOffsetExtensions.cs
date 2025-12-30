using Java.Time;

namespace Maui.Health.Platforms.Android.Extensions;

internal static class ZoneOffsetExtensions
{
    internal static ZoneOffset GetCurrent()
        => ZoneOffset.SystemDefault().Rules!.GetOffset(Instant.Now());
}
