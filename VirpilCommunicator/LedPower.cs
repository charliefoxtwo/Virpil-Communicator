namespace Virpil.Communicator
{
    public enum LedPower
    {
        /// <summary>
        /// Off
        /// </summary>
        Zero,
        /// <summary>
        /// Virpil says 30%, so even though it's really 25% we'll call it Thirty
        /// </summary>
        Thirty,
        /// <summary>
        /// Virpil says 60%, so even though it's really 50% we'll call it Sixty
        /// </summary>
        Sixty,
        /// <summary>
        /// 100%
        /// </summary>
        Full,
    }
}