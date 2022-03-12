using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using HidSharp;
using Microsoft.Extensions.Logging;

namespace Virpil.Communicator;

public sealed class VirpilMonitor : IVirpilMonitor
{
    public const ushort VID = 0x3344;

    public static readonly HashSet<ushort> ControlPanel1Pids = new() { 0x0259 };
    public static readonly HashSet<ushort> ControlPanel2Pids = new() { 0x025B, 0x825B };
    public static readonly HashSet<ushort> ThrottleCM2Pids = new() { 0x8193 };
    public static readonly HashSet<ushort> ThrottleCM3Pids = new() { 0x0194, 0x8194 };

    private readonly ConcurrentDictionary<ushort, ConcurrentDictionary<string, IVirpilDevice>> _devices = new();

    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<VirpilMonitor> _log;
    private readonly HashSet<ushort> _allVids;

    private static volatile VirpilMonitor? _instance;

    private static readonly object InitLock = new();

    private VirpilMonitor(ILoggerFactory loggerFactory, IEnumerable<ushort>? extraVids)
    {
        _loggerFactory = loggerFactory;
        _log = loggerFactory.CreateLogger<VirpilMonitor>();
        _allVids = new HashSet<ushort>(extraVids ?? Enumerable.Empty<ushort>()) { VID };
        DeviceList.Local.Changed += OnDeviceListChanged;
    }

    /// <summary>
    /// Initializes a new instance of VirpilMonitor. Calling this method multiple times will have no effect and will
    /// return the already-initialized instance.
    /// </summary>
    /// <param name="loggerFactory">LoggerFactory used when creating loggers for new VirpilDevice instances</param>
    /// <param name="extraVids">Extra VIDs to monitor. Virpil default VID does not need to be included.</param>
    /// <returns>Instance of VirpilMonitor</returns>
    public static VirpilMonitor Initialize(ILoggerFactory loggerFactory, IEnumerable<ushort>? extraVids = null)
    {
        if (_instance is not null) return _instance;

        lock (InitLock)
        {
            if (_instance is not null) return _instance;
            _instance = new VirpilMonitor(loggerFactory, extraVids);
            DeviceList.Local.RaiseChanged();

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

    /// <inheritdoc />
    public bool TryGetDevice(ushort pid, [MaybeNullWhen(false)] out IVirpilDevice virpilDevice)
    {
        return TryGetDevice(pid, null, out virpilDevice);
    }

    /// <inheritdoc />
    public bool TryGetDevice(ushort pid, string? deviceName, [MaybeNullWhen(false)] out IVirpilDevice virpilDevice)
    {
        virpilDevice = null;
        if (!_devices.TryGetValue(pid, out var pidMatches)) return false;

        if (deviceName is not null) return pidMatches.TryGetValue(deviceName, out virpilDevice);

        virpilDevice = pidMatches.First().Value;
        return true;
    }

    /// <inheritdoc />
    public ICollection<IVirpilDevice> AllConnectedVirpilDevices => _devices.SelectMany(d => d.Value.Values).ToArray();

    private long _changeCounter;
    private readonly object _changeLock = new();

    private void OnDeviceListChanged(object? sender, DeviceListChangedEventArgs e)
    {
        if (sender is not DeviceList list) return;

        var position = Interlocked.Increment(ref _changeCounter);

        lock (_changeLock)
        {
            // if we are the most recent call, continue, otherwise exit.
            // if more items have queued up after this, they're more recent and we should respect them instead.
            if (position != Interlocked.Read(ref _changeCounter)) return;

            var virpilDevices = _allVids.SelectMany(v => list.GetHidDevices(v))
                .Where(d => d.GetMaxFeatureReportLength() > 0);

            var existingDevices =
                new HashSet<(ushort, string)>(_devices.SelectMany(pids =>
                    pids.Value.Keys.Select(name => (pids.Key, name))));

            foreach (var hidDevice in virpilDevices)
            {
                if (_devices.TryGetValue((ushort)hidDevice.ProductID, out var pids) &&
                    pids.ContainsKey(hidDevice.GetFriendlyName()))
                {
                    existingDevices.Remove(((ushort) hidDevice.ProductID, hidDevice.GetFriendlyName()));
                }
                else
                {
                    _log.LogInformation("Detected new device {DevicePid:x4} [{Name}]", hidDevice.ProductID, hidDevice.GetFriendlyName());

                    var device = new VirpilDevice(hidDevice, _loggerFactory.CreateLogger<VirpilDevice>());

                    _devices.AddOrUpdate(device.PID, _ => new ConcurrentDictionary<string, IVirpilDevice>(),
                        (_, dict) =>
                        {
                            dict.AddOrUpdate(device.DeviceName, device, (_, _) => device);
                            return dict;
                        });
                }
            }

            // TODO: update remove conditions
            foreach (var (pid, name) in existingDevices)
            {
                _log.LogInformation("Device removed {DevicePid:x4} [{Name}]", pid, name);
                if (_devices.TryGetValue(pid, out var oldPids) &&
                    oldPids.TryRemove(name, out _) && oldPids.IsEmpty)
                {
                    _devices.TryRemove(pid, out _);
                }
            }
        }
    }
}
