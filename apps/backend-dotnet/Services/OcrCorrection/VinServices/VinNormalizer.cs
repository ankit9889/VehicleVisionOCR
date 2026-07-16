using System.Collections.Generic;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces;

namespace VehicleVisionOCR.Backend.Services.OcrCorrection.VinServices
{
    /// <summary>
    /// Implementation of <see cref="IVinNormalizer"/>.
    /// Enforces ISO 3779 universal and positional character rules for VINs.
    /// </summary>
    public class VinNormalizer : IVinNormalizer
    {
        /// <inheritdoc/>
        public (string Normalized, List<string> AppliedRules) NormalizeUniversalRules(string rawText)
        {
            var rules = new List<string>();
            if (string.IsNullOrEmpty(rawText)) return (rawText, rules);

            // Single pass allocation-free character filtering/replacement
            Span<char> buffer = stackalloc char[rawText.Length];
            int index = 0;
            bool appliedUniversalMap = false;

            for (int i = 0; i < rawText.Length; i++)
            {
                char c = char.ToUpperInvariant(rawText[i]);
                
                if (c == ' ' || c == '-') continue;

                if (c == 'O' || c == 'Q')
                {
                    c = '0';
                    appliedUniversalMap = true;
                }
                else if (c == 'I')
                {
                    c = '1';
                    appliedUniversalMap = true;
                }

                buffer[index++] = c;
            }

            if (appliedUniversalMap)
            {
                rules.Add("Universal ISO-3779 Map (O->0, I->1, Q->0)");
            }

            return (new string(buffer.Slice(0, index)), rules);
        }

        /// <inheritdoc/>
        public (string Normalized, List<string> AppliedRules) NormalizeStructuralRules(string candidate)
        {
            var rules = new List<string>();
            if (candidate.Length != 17) return (candidate, rules);

            char[] chars = candidate.ToCharArray();
            bool structuralChanged = false;

            // Characters 12-17 (VIS) must be numeric
            for (int i = 11; i < 17; i++)
            {
                if (char.IsLetter(chars[i]))
                {
                    char mapped = MapLetterToNumber(chars[i]);
                    if (mapped != chars[i])
                    {
                        chars[i] = mapped;
                        structuralChanged = true;
                    }
                }
            }

            if (structuralChanged)
            {
                rules.Add("VIS Positional Numeric Map (Pos 12-17)");
            }

            return (new string(chars), rules);
        }

        private char MapLetterToNumber(char c)
        {
            return c switch
            {
                'S' => '5',
                'Z' => '2',
                'B' => '8',
                'G' => '6',
                'T' => '7',
                'D' => '0',
                _ => c
            };
        }
    }
}
