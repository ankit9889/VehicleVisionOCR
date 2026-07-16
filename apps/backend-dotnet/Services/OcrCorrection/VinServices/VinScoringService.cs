using System.Collections.Generic;
using System.Text.RegularExpressions;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Helpers;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces;

namespace VehicleVisionOCR.Backend.Services.OcrCorrection.VinServices
{
    /// <summary>
    /// Implementation of <see cref="IVinScoringService"/>.
    /// Uses a 40/30/30 weighting model (OCR Confidence, Pattern Integrity, Math Check Digit).
    /// </summary>
    public partial class VinScoringService : IVinScoringService
    {
        [GeneratedRegex("^[A-HJ-NPR-Z0-9]{17}$")]
        private static partial Regex VinPatternRegex();

        /// <inheritdoc/>
        public double ScoreCandidate(string candidate, string rawOcrText, double ocrConfidence, List<string> knownWmis)
        {
            double score = 0;

            // 1. OCR Confidence Baseline (Weight: 40%)
            score += ocrConfidence * 0.40;

            // 2. Exact Pattern Match (Weight: 30%)
            if (candidate.Length == 17 && VinPatternRegex().IsMatch(candidate))
            {
                score += 30.0;
            }

            // 3. WMI Verification Bonus (Weight: +5%)
            if (candidate.Length >= 3 && knownWmis != null && knownWmis.Contains(candidate.Substring(0, 3)))
            {
                score += 5.0;
            }

            // 4. ISO 3779 Check Digit (Weight: 30%)
            if (candidate.Length == 17)
            {
                bool isCheckDigitValid = VinCheckDigitCalculator.Validate(candidate);
                if (isCheckDigitValid)
                {
                    score += 30.0;
                }
                else
                {
                    // Severe penalty if check digit fails but string looks otherwise perfect
                    score -= 15.0;
                }
            }

            // Normalize max score to 100
            return score > 100.0 ? 100.0 : score;
        }
    }
}
