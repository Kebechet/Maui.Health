using Java.Time;

namespace Maui.Health.Platforms.Android.Extensions;

// Only called from Health Connect code paths which require API 26+
#pragma warning disable CA1416
internal static class ZoneOffsetExtensions
{
    // Java bindings declare nullable returns but these are never null on real Android devices
    internal static ZoneOffset GetCurrent()
        => ZoneOffset.SystemDefault()!.Rules!.GetOffset(Instant.Now())!;

    /// <summary>
    /// Converts a Health Connect record's recording <see cref="ZoneOffset"/> to a
    /// <see cref="TimeSpan"/>. Null when the record didn't store an offset (older Health
    /// Connect versions can omit it for manual entries).
    /// </summary>
    internal static TimeSpan? ToTimeSpan(this ZoneOffset? zoneOffset)
        => zoneOffset is null
            ? null
            : TimeSpan.FromSeconds(zoneOffset.TotalSeconds);
}
#pragma warning restore CA1416
