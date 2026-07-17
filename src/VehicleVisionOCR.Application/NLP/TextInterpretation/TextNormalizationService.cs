using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using VehicleVisionOCR.Domain.NLP.Interfaces;
using VehicleVisionOCR.Domain.NLP.Models;

namespace VehicleVisionOCR.Application.NLP.TextInterpretation
{
    public class TextNormalizationService : ITextNormalizationService
    {
        public string Normalize(string rawText, InterpretationProfile config)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;

            string normalized = rawText.Trim();

            // 1. Remove diacritics / unicode normalization
            if (config.EnableUnicodeNormalization)
            {
                normalized = RemoveDiacritics(normalized);
            }

            // 2. OCR Confusion Repair (Domain independent character replacements)
            // Example: Lowercase 'l' often misread instead of uppercase 'I' in all-caps scenarios.
            if (config.EnableOcrConfusionRepair && config.OcrConfusionMatrix != null)
            {
                foreach (var kvp in config.OcrConfusionMatrix)
                {
                    normalized = normalized.Replace(kvp.Key, kvp.Value);
                }
            }

            // 3. Condense multiple whitespaces
            normalized = Regex.Replace(normalized, @"\s+", " ");

            // 4. Strip non-printable noise
            normalized = Regex.Replace(normalized, @"[^\x20-\x7E]+", "");

            return normalized;
        }

        private string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
