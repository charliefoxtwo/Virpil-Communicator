using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Virpil.Communicator;

public interface IVirpilMonitor
{
    /// <summary>
    /// Attempts to fetch a device, if it exists.
    /// </summary>
    /// <param name="pid">The PID of the device to fetch</param>
    /// <param name="serialNumber">The serial number of the USB device, or null to ignore</param>
    /// <param name="virpilDevice">The device, if exactly a single device is found, otherwise null</param>
    /// <returns><code>true</code> if exactly one device is found matching the parameters, otherwise false</returns>
    bool TryGetDevice(ushort pid, string? serialNumber, [MaybeNullWhen(false)] out IVirpilDevice virpilDevice);

    /// <summary>
    /// Enumerates all usb devices with the Virpil VID connected to the system
    /// </summary>
    /// <returns>All connected virpil devices</returns>
    ICollection<IVirpilDevice> AllConnectedVirpilDevices { get; }
}
