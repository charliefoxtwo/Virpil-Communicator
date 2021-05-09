namespace Virpil.Communicator
{
    public enum BoardType : byte
    {
        /// <summary>
        /// Not necessarily a board type, but is used when setting the LEDs to default
        /// </summary>
        Default = 0x64,
        AddBoard = 0x65,
        /// <summary>
        /// LEDs which are on the board that is directly connected to USB
        /// </summary>
        OnBoard = 0x66,
        /// <summary>
        /// LEDs which are on a slave board connected to a parent board which is directly connected to USB
        /// </summary>
        SlaveBoard = 0x67,
    }
}