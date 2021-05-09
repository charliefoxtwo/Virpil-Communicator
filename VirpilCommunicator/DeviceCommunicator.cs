using System;
using System.Linq;
using HidLibrary;
using Microsoft.Extensions.Logging;

namespace Virpil.Communicator
{
    public class DeviceCommunicator
    {
        public const ushort VID = 0x3344;
        public const string ControlPanel1Pid = "0259";
        public const string ControlPanel2Pid = "825B";
        public const string ThrottleCM2Pid = "8193";

        private readonly HidDevice _device;

        private readonly ILogger<DeviceCommunicator> _log;

        /// <summary>
        /// Creates a new device communicator with the first HID device matching the vendor id and product id and which
        /// has <code>FeatureReportByteLength > 0</code>. In theory this should always return the correct device.
        /// </summary>
        /// <param name="pid">The product id, e.g. <see cref="ControlPanel2Pid"/> or <see cref="ThrottleCM2Pid"/></param>
        /// <param name="log">Logger for use by the class</param>
        public DeviceCommunicator(ushort pid, ILogger<DeviceCommunicator> log)
        {
            _device = HidDevices.Enumerate(VID, pid).First(d => d.Capabilities.FeatureReportByteLength > 0);
            _log = log;
        }

        public bool SendCommand(BoardType boardType, int ledNumber, LedPower red, LedPower green, LedPower blue)
        {
            var packet = PacketForCommand(boardType, ledNumber, red, green, blue);
            _log.LogDebug($"Sending {red}, {green}, {blue} to {boardType} #{ledNumber}", red, green, blue, boardType, ledNumber);
            return _device.WriteFeatureData(packet);
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
    }
}