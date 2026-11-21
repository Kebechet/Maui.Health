namespace Maui.Health.Constants;

/// <summary>
/// Metadata key constants for workout sessions.
/// </summary>
public static class WorkoutMetadata
{
    /// <summary>
    /// Active duration in seconds.
    /// </summary>
    public const string ActiveDurationSeconds = nameof(ActiveDurationSeconds);

    /// <summary>
    /// Paused duration in seconds.
    /// </summary>
    public const string PausedDurationSeconds = nameof(PausedDurationSeconds);

    /// <summary>
    /// Number of pauses.
    /// </summary>
    public const string PauseCount = nameof(PauseCount);

    /// <summary>
    /// Pause intervals data.
    /// </summary>
    public const string PauseIntervals = nameof(PauseIntervals);
}
