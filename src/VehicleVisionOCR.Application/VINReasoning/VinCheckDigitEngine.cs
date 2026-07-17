using System;
using System.Collections.Generic;
using VehicleVisionOCR.Domain.VIN.Interfaces;

namespace VehicleVisionOCR.Application.VINReasoning
{
    public class VinCheckDigitEngine : IVinCheckDigitEngine
    {
        private static readonly Dictionary<char, int> CharValues = new Dictionary<char, int>
        {
            {'A', 1}, {'B', 2}, {'C', 3}, {'D', 4}, {'E', 5}, {'F', 6}, {'G', 7}, {'H', 8},
            {'J', 1}, {'K', 2}, {'L', 3}, {'M', 4}, {'N', 5}, {'P', 7}, {'R', 9},
            {'S', 2}, {'T', 3}, {'U', 4}, {'V', 5}, {'W', 6}, {'X', 7}, {'Y', 8}, {'Z', 9},
            {'1', 1}, {'2', 2}, {'3', 3}, {'4', 4}, {'5', 5}, {'6', 6}, {'7', 7}, {'8', 8}, {'9', 9}, {'0', 0}
        };

        private static readonly int[] Weights = { 8, 7, 6, 5, 4, 3, 2, 10, 0, 9, 8, 7, 6, 5, 4, 3, 2 };

        public bool Calculate(string vin)
        {
            if (string.IsNullOrEmpty(vin) || vin.Length != 17)
                return false;

            int sum = 0;
            for (int i = 0; i < 17; i++)
            {
                char c = vin[i];
                if (!CharValues.ContainsKey(c))
                    return false; // Invalid character found

                sum += CharValues[c] * Weights[i];
            }

            int remainder = sum % 11;
            char expectedCheckDigit = remainder == 10 ? 'X' : (char)('0' + remainder);

            return vin[8] == expectedCheckDigit;
        }
    }
}
