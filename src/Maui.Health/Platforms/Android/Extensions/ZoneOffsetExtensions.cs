using Java.Time;

namespace Maui.Health.Platforms.Android.Extensions;

// Only called from Health Connect code paths which require API 26+
#pragma warning disable CA1416
internal static class ZoneOffsetExtensions
{
    // Java bindings declare nullable returns but these are never null on real Android devices
    internal static ZoneOffset GetCurrent()
        => ZoneOffset.SystemDefault()!.Rules!.GetOffset(Instant.Now())!;
}
#pragma warning restore CA1416
