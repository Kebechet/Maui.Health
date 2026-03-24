using Java.Time;

namespace Maui.Health.Platforms.Android.Extensions;

internal static class ZoneOffsetExtensions
{
    [System.Runtime.Versioning.SupportedOSPlatform("android26.0")]
    // Java bindings declare nullable returns but these are never null on real Android devices
    internal static ZoneOffset GetCurrent()
        => ZoneOffset.SystemDefault()!.Rules!.GetOffset(Instant.Now())!;
}
