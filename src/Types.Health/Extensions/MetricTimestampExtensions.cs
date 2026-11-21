namespace Maui.Health.Extensions;

/// <summary>
/// Helpers that rebase a UTC instant onto a recording offset without changing the absolute moment.
/// HealthKit and Health Connect both store the absolute instant in UTC and the recording offset
/// separately (HealthKit via <c>HKMetadataKeyTimeZone</c>; Health Connect via the record's
/// <c>ZoneOffset</c>). The platform read paths use this helper to fold the offset back into the
/// returned <see cref="DateTimeOffset"/> so consumers see the wall-clock time of the recording,
/// not the UTC clock time.
/// </summary>
public static class MetricTimestampExtensions
{
    /// <summary>
    /// Returns <paramref name="utcInstant"/> rebased to <paramref name="offset"/>. The absolute
    /// moment is preserved; only the carried offset changes. A null offset returns the input
    /// unchanged — used when the source record has no recording-zone metadata.
    /// </summary>
    public static DateTimeOffset RebaseToOffset(this DateTimeOffset utcInstant, TimeSpan? offset)
    {
        if(offset is null)
        {
            return utcInstant;
        }

        return utcInstant.ToOffset(offset.Value);
    }

    /// <summary>
    /// Returns <paramref name="moment"/> rebased to the offset of <paramref name="timeZone"/>
    /// at that moment. The absolute instant is preserved; only the carried offset changes.
    /// DST-aware: on a spring-forward day, midnight in CEST returns <c>+01:00</c> because the
    /// transition happens at 02:00; on a fall-back day, midnight returns <c>+02:00</c>. Used by
    /// the iOS aggregator to surface bucket boundaries with the user's local offset instead of
    /// UTC <c>+00:00</c>, matching the Android path.
    /// </summary>
    public static DateTimeOffset RebaseToZone(this DateTimeOffset moment, TimeZoneInfo timeZone)
        => TimeZoneInfo.ConvertTime(moment, timeZone);
}
