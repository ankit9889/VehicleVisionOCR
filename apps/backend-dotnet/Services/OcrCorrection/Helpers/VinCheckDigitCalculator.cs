using System.Collections.Generic;

namespace VehicleVisionOCR.Backend.Services.OcrCorrection.Helpers
{
    /// <summary>
    /// Static utility for validating Vehicle Identification Numbers against the ISO 3779 Standard Check Digit algorithm.
    /// </summary>
    public static class VinCheckDigitCalculator
    {
        private static int GetCharValue(char c)
        {
            return c switch
            {
                'A' or 'J' or '1' => 1,
                'B' or 'K' or 'S' or '2' => 2,
                'C' or 'L' or 'T' or '3' => 3,
                'D' or 'M' or 'U' or '4' => 4,
                'E' or 'N' or 'V' or '5' => 5,
                'F' or 'W' or '6' => 6,
                'G' or 'P' or 'X' or '7' => 7,
                'H' or 'Y' or '8' => 8,
                'R' or 'Z' or '9' => 9,
                '0' => 0,
                _ => -1 // Invalid character
            };
        }

        private static readonly int[] Weights = { 8, 7, 6, 5, 4, 3, 2, 10, 0, 9, 8, 7, 6, 5, 4, 3, 2 };

        /// <summary>
        /// Validates a 17-character VIN by transliterating characters to numbers, applying standard weights,
        /// and calculating modulus 11.
        /// </summary>
        /// <param name="vin">The 17-character VIN candidate string.</param>
        /// <returns>True if the mathematically derived check digit matches the 9th character in the VIN; otherwise, false.</returns>
        public static bool Validate(string vin)
        {
            if (string.IsNullOrWhiteSpace(vin) || vin.Length != 17) return false;

            int sum = 0;
            for (int i = 0; i < 17; i++)
            {
                char c = vin[i];
                int val = GetCharValue(c);
                if (val == -1)
                {
                    return false; // Contains invalid character (I, O, Q, or special)
                }
                sum += val * Weights[i];
            }

            int remainder = sum % 11;
            char expectedCheckDigit = remainder == 10 ? 'X' : (char)('0' + remainder);

            return vin[8] == expectedCheckDigit;
        }
    }
}
