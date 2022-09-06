namespace Virpil.Communicator;

public enum BoardType : byte
{
    /// <summary>
    /// Not necessarily a board type, but is used when setting the LEDs to default
    /// </summary>
    Default = 0x64,
    /// <summary>
    /// LEDs which are part of a board that is directly attached to the target board (e.g. a joystick)
    /// </summary>
    AddBoard = 0x65,
    /// <summary>
    /// LEDs which are on the board that is directly connected to USB
    /// </summary>
    OnBoard = 0x66,
    /// <summary>
    /// LEDs which are on a slave board connected to a parent board which is directly connected to USB
    /// </summary>
    SlaveBoard = 0x67,
    /// <summary>
    /// LEDs used by boards such as the Alpha Prime (why? who knows!)
    /// </summary>
    ExtraBoard = 0x68,
}
