namespace Maui.Health.Models;

/// <summary>
/// Base class for health records with an identifier and data source.
/// </summary>
public class HealthRecord
{
    /// <summary>
    /// Unique identifier of the health record in the platform data store.
    /// </summary>
    public required string Id { get; set; }
    /// <summary>
    /// The app or device that created this record (e.g., package name).
    /// </summary>
    public required string DataOrigin { get; set; }
}
