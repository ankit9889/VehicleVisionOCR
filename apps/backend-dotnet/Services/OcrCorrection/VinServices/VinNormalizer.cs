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

            Span<char> buffer = stackalloc char[rawText.Length];
            int index = 0;
            bool strippedWhitespace = false;

            for (int i = 0; i < rawText.Length; i++)
            {
                char c = char.ToUpperInvariant(rawText[i]);
                if (c == ' ' || c == '-')
                {
                    strippedWhitespace = true;
                    continue;
                }
                buffer[index++] = c;
            }

            if (strippedWhitespace)
            {
                rules.Add("Stripped Whitespace/Hyphens");
            }

            return (new string(buffer.Slice(0, index)), rules);
        }

        /// <inheritdoc/>
        public (string Normalized, List<string> AppliedRules) NormalizeStructuralRules(string candidate)
        {
            // The Character Verification Layer (Confusion Matrix) now handles structural ambiguities.
            // We preserve the raw OCR text here.
            return (candidate, new List<string>());
        }
    }
}
