using System;
using System.Collections.Generic;
using System.Linq;
using HidSharp;
using Microsoft.Extensions.Logging;

namespace Virpil.Communicator
{
    public class DeviceCommunicator : IDisposable
    {
        public const ushort VID = 0x3344;

        public readonly HashSet<ushort> ControlPanel1Pids = new() { 0x0259 };
        public readonly HashSet<ushort> ControlPanel2Pids = new() { 0x025B, 0x825B };
        public readonly HashSet<ushort> ThrottleCM2Pids = new() { 0x8193 };
        public readonly HashSet<ushort> ThrottleCM3Pids = new() { 0x0194, 0x8194 };

        [Obsolete("Use ControlPanel1Pids instead")]
        public const string ControlPanel1Pid = "0259";
        [Obsolete("Use ControlPanel2Pids instead")]
        public const string ControlPanel2Pid = "825B";
        [Obsolete("Use ThrottleCM2Pids instead")]
        public const string ThrottleCM2Pid = "8193";
        [Obsolete("Use ThrottleCM3Pids instead")]
        public const string ThrottleCM3Pid = "8194";

        public ushort PID { get; }

        private readonly HidStream? _stream;

        private readonly ILogger<DeviceCommunicator> _log;

        /// <summary>
        /// Creates a new device communicator with the first HID device matching the vendor id and product id and which
        /// has <code>FeatureReportByteLength > 0</code>. In theory this should always return the correct device.
        /// </summary>
        /// <param name="pid">The product id, e.g. <see cref="ControlPanel2Pid"/> or <see cref="ThrottleCM2Pid"/></param>
        /// <param name="log">Logger for use by the class</param>
        public DeviceCommunicator(ushort pid, ILogger<DeviceCommunicator> log) : this(
            DeviceList.Local.GetHidDevices(VID, pid).FirstOrDefault(d => d.GetMaxFeatureReportLength() > 0), log)
        {

        }

        private DeviceCommunicator(HidDevice? device, ILogger<DeviceCommunicator> log)
        {
            PID = (ushort) (device?.ProductID ?? 0);
            _stream = device?.Open();
            _log = log;
        }

        /// <summary>
        /// Sends an LED command to the device
        /// </summary>
        /// <param name="boardType">The type of board the command is being sent to</param>
        /// <param name="ledNumber">The LED number</param>
        /// <param name="red">Power of the red hue</param>
        /// <param name="green">Power of the green hue</param>
        /// <param name="blue">Power of the blue hue</param>
        /// <returns></returns>
        public bool SendCommand(BoardType boardType, int ledNumber, LedPower red, LedPower green, LedPower blue)
        {
            if (_stream is null) return false;

            var packet = PacketForCommand(boardType, ledNumber, red, green, blue);
            _log.LogDebug("Sending {Red}, {Green}, {Blue} to {BoardType} #{LedNumber}", red, green, blue, boardType, ledNumber);

            _stream.SetFeature(packet);
            return true;
        }

        /// <summary>
        /// Enumerates all usb devices with the Virpil VID connected to the system
        /// </summary>
        /// <param name="loggerFactory">factory to create device loggers from</param>
        /// <returns>All connected virpil devices</returns>
        public static IEnumerable<DeviceCommunicator> AllConnectedVirpilDevices(ILoggerFactory loggerFactory)
        {
            return DeviceList.Local.GetHidDevices(VID).Where(d => d.GetMaxFeatureReportLength() > 0).Select(d =>
                new DeviceCommunicator(d, loggerFactory.CreateLogger<DeviceCommunicator>()));
        }

        private static byte[] PacketForCommand(BoardType boardType, int ledNumber, LedPower red, LedPower green, LedPower blue)
        {
            var data = new byte[38];
            data[0] = 0x02;
            data[1] = (byte) boardType;
            data[2] = CommandIdForCommand(boardType, ledNumber);
            data[ledNumber + 4] = ByteForColors(red, green, blue);
            data[37] = 0xf0;

            return data;
        }

        private static byte ByteForColors(LedPower red, LedPower green, LedPower blue)
        {
            byte b = 0b_1000_0000;
            b |= ByteForColor(red);
            b |= (byte)(ByteForColor(green) << 2);
            b |= (byte)(ByteForColor(blue) << 4);
            return b;
        }

        private static byte ByteForColor(LedPower color)
        {
            return color switch
            {
                LedPower.Zero => 0,
                LedPower.Thirty => 1,
                LedPower.Sixty => 2,
                LedPower.Full => 3,
                _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
            };
        }

        private static byte CommandIdForCommand(BoardType boardType, int ledNumber)
        {
            return boardType switch
            {
                BoardType.Default => 0,
                BoardType.AddBoard => (byte) ledNumber,
                BoardType.OnBoard => (byte) (4 + ledNumber),
                BoardType.SlaveBoard => (byte) (24 + ledNumber),
                _ => throw new ArgumentOutOfRangeException(nameof(boardType), boardType, null)
            };
        }

        public void Dispose()
        {
            _stream?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}