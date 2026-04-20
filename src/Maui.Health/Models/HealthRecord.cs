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
    /// Stable identifier of the app that produced this record.
    /// iOS: <c>HKSource.BundleIdentifier</c>. Android: Health Connect <c>DataOrigin.PackageName</c>.
    /// <c>null</c> when the platform exposes no source metadata.
    /// </summary>
    public string? DataOrigin { get; set; }
}
