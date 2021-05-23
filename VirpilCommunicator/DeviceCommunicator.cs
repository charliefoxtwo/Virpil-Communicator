using System;
using System.Linq;
using HidSharp;
using Microsoft.Extensions.Logging;

namespace Virpil.Communicator
{
    public class DeviceCommunicator : IDisposable
    {
        public ushort PID { get; }

        private readonly HidStream? _stream;

        private readonly ILogger<DeviceCommunicator> _log;

        /// <summary>
        /// Creates a new device communicator with the first HID device matching the vendor id and product id and which
        /// has <code>FeatureReportByteLength > 0</code>. In theory this should always return the correct device.
        /// </summary>
        /// <param name="pid">The product id, e.g. <see cref="VirpilMonitor.ControlPanel2Pids"/></param>
        /// <param name="log">Logger for use by the class</param>
        public DeviceCommunicator(ushort pid, ILogger<DeviceCommunicator> log) : this(
            DeviceList.Local.GetHidDevices(VirpilMonitor.VID, pid).FirstOrDefault(d => d.GetMaxFeatureReportLength() > 0), log)
        {

        }

        internal DeviceCommunicator(HidDevice? device, ILogger<DeviceCommunicator> log)
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