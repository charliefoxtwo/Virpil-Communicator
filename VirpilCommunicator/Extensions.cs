using System;
using System.Collections.Generic;

namespace Virpil.Communicator
{
    public static class Extensions
    {
        private static readonly Dictionary<string, LedPower> LedPowerStrings = new(StringComparer.InvariantCultureIgnoreCase)
        {
            ["00"] = LedPower.Zero,
            ["40"] = LedPower.Thirty,
            ["80"] = LedPower.Sixty,
            ["FF"] = LedPower.Full,
        };

        public static string AsHexString(LedPower power)
        {
            return power switch
            {
                LedPower.Zero => "0",
                LedPower.Thirty => "40",
                LedPower.Sixty => "80",
                LedPower.Full => "FF",
                _ => throw new ArgumentOutOfRangeException(nameof(power), power, null)
            };
        }

        public static (LedPower Red, LedPower Green, LedPower Blue) ToLedPowers(this string color)
        {
            if (!LedPowerStrings.TryGetValue(color[..2], out var red) ||
                !LedPowerStrings.TryGetValue(color[2..4], out var green) ||
                !LedPowerStrings.TryGetValue(color[4..6], out var blue))
            {
                throw new ArgumentException($"argument must be formatted in 6-bit hex color (got {color})", nameof(color));
            }

            return (red, green, blue);
        }
    }
}