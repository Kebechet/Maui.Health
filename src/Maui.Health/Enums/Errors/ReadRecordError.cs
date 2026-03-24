namespace Maui.Health.Enums.Errors;

/// <summary>
/// Errors that can occur when reading health records from the data store.
/// </summary>
public enum ReadRecordError
{
    /// <summary>
    /// The required permission was not granted or was revoked.
    /// </summary>
    PermissionProblem,
    /// <summary>
    /// An error occurred while reading the records from the health data store.
    /// </summary>
    ProblemDuringReading,
}
