using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Virpil.Communicator;

public interface IVirpilMonitor
{
    /// <summary>
    /// Attempts to fetch a device, if it exists.
    /// </summary>
    /// <param name="pid">The PID of the device to fetch</param>
    /// <param name="virpilDevice">The device, if found, otherwise null</param>
    /// <returns></returns>
    bool TryGetDevice(ushort pid, [MaybeNullWhen(false)] out IVirpilDevice virpilDevice);

    /// <summary>
    /// Enumerates all usb devices with the Virpil VID connected to the system
    /// </summary>
    /// <returns>All connected virpil devices</returns>
    ICollection<IVirpilDevice> AllConnectedVirpilDevices { get; }
}
