namespace Maui.Health.Models;

/// <summary>
/// Represents device hardware and software metadata associated with a health record
/// </summary>
public class DeviceDetail
{
    /// <summary>
    /// Type of device (e.g., Phone, Watch, FitnessTracker)
    /// </summary>
    public string? DeviceType { get; init; }

    /// <summary>
    /// Device manufacturer (e.g., Apple, Samsung, Google)
    /// </summary>
    public string? Manufacturer { get; init; }

    /// <summary>
    /// Device model name
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Hardware version of the device (iOS only)
    /// </summary>
    public string? HardwareVersion { get; init; }

    /// <summary>
    /// Software/OS version of the device (iOS only)
    /// </summary>
    public string? SoftwareVersion { get; init; }

    /// <summary>
    /// Device name (iOS only)
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Unique device identifier (iOS only)
    /// </summary>
    public string? Id { get; init; }
}
