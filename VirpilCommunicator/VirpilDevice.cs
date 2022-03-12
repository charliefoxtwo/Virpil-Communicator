using System;
using HidSharp;
using Microsoft.Extensions.Logging;

namespace Virpil.Communicator;

public class VirpilDevice : IVirpilDevice, IDisposable
{
    public ushort PID { get; }

    public string DeviceName { get; }

    private readonly HidStream _stream;

    private readonly ILogger<VirpilDevice> _log;

    public VirpilDevice(HidDevice device, ILogger<VirpilDevice> log)
    {
        PID = (ushort) device.ProductID;
        DeviceName = device.GetFriendlyName();
        _stream = device.Open();
        _log = log;
    }

    /// <inheritdoc />
    public bool SendCommand(BoardType boardType, int ledNumber, LedPower red, LedPower green, LedPower blue)
    {
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
        _stream.Dispose();
        GC.SuppressFinalize(this);
    }
}
