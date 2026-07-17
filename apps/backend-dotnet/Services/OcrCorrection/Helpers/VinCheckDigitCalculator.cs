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
        /// Note: Check digit validation is only strictly enforced for North American and Chinese VINs.
        /// </summary>
        /// <param name="vin">The 17-character VIN candidate string.</param>
        /// <returns>True if valid or if from a region where check digits are not enforced; otherwise, false.</returns>
        public static bool Validate(string vin)
        {
            if (string.IsNullOrWhiteSpace(vin)) return false;
            if (vin.Length < 14 || vin.Length > 20) return false;

            char wmiRegion = vin[0];
            bool isCheckDigitMandatory = (wmiRegion == '1' || wmiRegion == '2' || wmiRegion == '3' || 
                                         wmiRegion == '4' || wmiRegion == '5' || wmiRegion == 'L') && vin.Length == 17;

            int sum = 0;
            for (int i = 0; i < vin.Length; i++)
            {
                char c = vin[i];
                int val = GetCharValue(c);
                if (val == -1)
                {
                    // Contains invalid character (I, O, Q, or special).
                    return false; 
                }
                if (vin.Length == 17)
                {
                    sum += val * Weights[i];
                }
            }

            if (vin.Length == 16) return true; // 16-char Asian chassis numbers bypass ISO check digit math

            int remainder = sum % 11;
            char expectedCheckDigit = remainder == 10 ? 'X' : (char)('0' + remainder);

            bool matches = vin[8] == expectedCheckDigit;
            
            // If it matches, it's valid. If it doesn't match but isn't mandatory, it's still considered valid.
            return matches || !isCheckDigitMandatory;
        }
    }
}
