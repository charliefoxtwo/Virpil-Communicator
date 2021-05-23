using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HidSharp;
using Microsoft.Extensions.Logging;

namespace Virpil.Communicator
{
    public class VirpilMonitor
    {
        public const ushort VID = 0x3344;

        public static readonly HashSet<ushort> ControlPanel1Pids = new() { 0x0259 };
        public static readonly HashSet<ushort> ControlPanel2Pids = new() { 0x025B, 0x825B };
        public static readonly HashSet<ushort> ThrottleCM2Pids = new() { 0x8193 };
        public static readonly HashSet<ushort> ThrottleCM3Pids = new() { 0x0194, 0x8194 };

        private readonly ConcurrentDictionary<ushort, DeviceCommunicator> _devices;

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<VirpilMonitor> _log;

        private static volatile VirpilMonitor? _instance;

        private static readonly object InitLock = new();

        private VirpilMonitor(ILoggerFactory loggerFactory, IEnumerable<DeviceCommunicator> devices)
        {
            DeviceList.Local.Changed += OnDeviceListChanged;
            _loggerFactory = loggerFactory;
            _log = loggerFactory.CreateLogger<VirpilMonitor>();
            _devices = new ConcurrentDictionary<ushort, DeviceCommunicator>(devices.ToDictionary(d => d.PID));
        }

        /// <summary>
        /// Initializes a new instance of VirpilMonitor. Calling this method multiple times will have no effect and will
        /// return the already-initialized instance.
        /// </summary>
        /// <param name="loggerFactory">LoggerFactory used when creating loggers for new DeviceCommunicator instances</param>
        /// <returns>Instance of VirpilMonitor</returns>
        public static VirpilMonitor Initialize(ILoggerFactory loggerFactory)
        {
            if (_instance is not null) return _instance;

            lock (InitLock)
            {
                if (_instance is not null) return _instance;

                var devices = DeviceList.Local.GetHidDevices(VID).Where(d => d.GetMaxFeatureReportLength() > 0).Select(d =>
                    new DeviceCommunicator(d, loggerFactory.CreateLogger<DeviceCommunicator>()));
                _instance = new VirpilMonitor(loggerFactory, devices);
                return _instance;
            }
        }

        /// <summary>
        /// Checks to see whether or not VirpilMonitor has finished initialization
        /// </summary>
        public static bool IsInitialized()
        {
            lock (InitLock)
            {
                return _instance is not null;
            }
        }

        /// <summary>
        /// Returns the current VirpilMonitor instance, or null if not initialized.
        /// </summary>
        public static VirpilMonitor? Instance
        {
            get
            {
                lock (InitLock)
                {
                    return _instance;
                }
            }
        }

        /// <summary>
        /// Attempts to fetch a device, if it exists.
        /// </summary>
        /// <param name="pid">The PID of the device to fetch</param>
        /// <param name="device">The device, if found, otherwise null</param>
        /// <returns></returns>
        public bool TryGetDevice(ushort pid, [MaybeNullWhen(false)] out DeviceCommunicator device)
        {
            return _devices.TryGetValue(pid, out device);
        }

        /// <summary>
        /// Enumerates all usb devices with the Virpil VID connected to the system
        /// </summary>
        /// <returns>All connected virpil devices</returns>
        public ICollection<DeviceCommunicator> AllConnectedVirpilDevices => _devices.Values;

        private void OnDeviceListChanged(object? sender, DeviceListChangedEventArgs e)
        {
            if (sender is not DeviceList list) return;

            var virpilDevices = list.GetHidDevices(VID).Where(d => d.GetMaxFeatureReportLength() > 0);

            var existingDevices = new HashSet<ushort>(_devices.Keys);

            foreach (var device in virpilDevices)
            {
                if (_devices.TryGetValue((ushort) device.ProductID, out _))
                {
                    existingDevices.Remove((ushort) device.ProductID);
                }
                else
                {
                    _log.LogInformation("Detected new device {DevicePid:x4}", device.ProductID);
                    _devices.TryAdd((ushort) device.ProductID,
                        new DeviceCommunicator(device, _loggerFactory.CreateLogger<DeviceCommunicator>()));
                }
            }

            foreach (var oldDevice in existingDevices)
            {
                _log.LogInformation("Device removed {DevicePid:x4}", oldDevice);
                _devices.TryRemove(oldDevice, out _);
            }
        }
    }
}