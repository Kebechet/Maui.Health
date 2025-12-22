namespace Maui.Health.Enums;
/// <summary>
/// Represents the state of a workout session. Used internally to be able fully copy classic google/ios activity.
/// </summary>
public enum WorkoutSessionState
{
    /// <summary>
    /// Session is currently running
    /// </summary>
    Running,

    /// <summary>
    /// Session is paused
    /// </summary>
    Paused,

    /// <summary>
    /// Session has ended
    /// </summary>
    Ended
}