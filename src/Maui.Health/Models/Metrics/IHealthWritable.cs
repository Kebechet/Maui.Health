namespace Maui.Health.Models.Metrics;

/// <summary>
/// Marker interface for types that can be written to the health store.
/// Implemented by <see cref="HealthWriteData"/> (point-in-time) and <see cref="HealthWriteRangeData"/> (time range).
/// </summary>
public interface IHealthWritable;
