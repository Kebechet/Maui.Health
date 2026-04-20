using Maui.Health.Enums;
using Maui.Health.Models;

namespace Maui.Health.Models.Metrics;

/// <summary>
/// Base class for all health metric DTOs providing common properties
/// </summary>
public abstract class HealthMetricBase
{
    /// <summary>
    /// Unique identifier for this health record
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The health SDK that provided this record.
    /// </summary>
    public required HealthDataSdk DataSdk { get; init; }

    /// <summary>
    /// Stable identifier of the app that produced this record.
    /// iOS: <c>HKSource.BundleIdentifier</c> (e.g. <c>com.example.MyApp</c>).
    /// Android: Health Connect <c>DataOrigin.PackageName</c> (e.g. <c>com.example.myapp</c>).
    /// Safe to compare against the running app's own bundle/package identifier to determine ownership.
    /// <c>null</c> when the platform exposes no source metadata (edge case — e.g. a HealthKit sample
    /// with a null <c>SourceRevision</c>, or a synthesized aggregate DTO).
    /// </summary>
    public string? DataOrigin { get; init; }

    /// <summary>
    /// Timestamp when the measurement was taken or recorded
    /// For duration-based metrics, this typically represents the start time or a representative time
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// How the data was recorded (manual, automatic, etc.)
    /// </summary>
    public string? RecordingMethod { get; init; }

    /// <summary>
    /// Additional metadata for the record
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Device details associated with this health record
    /// </summary>
    public DeviceDetail? Device { get; init; }
}
