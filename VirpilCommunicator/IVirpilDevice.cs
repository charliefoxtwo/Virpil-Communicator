namespace Virpil.Communicator;

public interface IVirpilDevice
{
    ushort PID { get; }

    string DeviceName { get; }

    /// <summary>
    /// Sends an LED command to the device
    /// </summary>
    /// <param name="boardType">The type of board the command is being sent to</param>
    /// <param name="ledNumber">The LED number</param>
    /// <param name="red">Power of the red hue</param>
    /// <param name="green">Power of the green hue</param>
    /// <param name="blue">Power of the blue hue</param>
    /// <returns></returns>
    bool SendCommand(BoardType boardType, int ledNumber, LedPower red, LedPower green, LedPower blue);
}
