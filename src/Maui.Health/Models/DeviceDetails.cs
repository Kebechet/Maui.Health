namespace Maui.Health.Models;

public class DeviceDetails
{
    /// <summary>
    /// On Android this is from device metadata, Apple does not provide a type
    /// </summary>
    public string? DeviceType { get; set; }

    /// <summary>
    /// Manufacturer of the device, e.g. Apple, Samsung, etc.
    /// Available on Android and Apple.
    /// </summary>
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Model of the device, e.g. iPhone 13 Pro Max, Samsung Galaxy S21 Ultra, etc.
    /// Available on Android and Apple.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Hardware version of the device, e.g. 'Watch7,4' etc.
    /// Available on Apple only.
    /// </summary>
    public string? HardwareVersion { get; set; }

    /// <summary>
    /// Software version of the device, e.g. '15.4' etc.
    /// Available on Apple only.
    /// </summary>
    public string? SoftwareVersion { get; set; }

    /// <summary>
    /// Name of the device, e.g. 'iPhone 13 Pro Max'
    /// Available on Apple only.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Unique identifier for the device.
    /// Available on Apple only.
    /// </summary>
    public string? Id { get; set; }
}