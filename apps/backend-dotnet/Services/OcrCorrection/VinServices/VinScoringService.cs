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
        public double ScoreCandidate(Models.CandidateScore candidateObj, string rawOcrText, double ocrConfidence, List<string> knownWmis)
        {
            double score = 0;
            string candidate = candidateObj.Candidate;

            // 1. OCR Confidence Baseline (Weight: 40%)
            score += ocrConfidence * 0.40;

            // Character confusion probability penalty
            // Deduct 2 points for every character substituted via the Confusion Matrix.
            score -= (candidateObj.Substitutions * 2.0);

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
                    score -= 20.0;
                }
            }

            // 5. Positional Constraints Penalty
            if (candidate.Length == 17)
            {
                // I, O, Q are never allowed in a VIN
                for (int i = 0; i < 17; i++)
                {
                    if (candidate[i] == 'I' || candidate[i] == 'O' || candidate[i] == 'Q')
                    {
                        score -= 15.0;
                    }
                }

                // Characters 14-17 MUST be numeric globally (12-17 for NA, but we enforce 14-17 strictly)
                for (int i = 13; i < 17; i++)
                {
                    if (char.IsLetter(candidate[i]))
                    {
                        score -= 10.0;
                    }
                }
                
                // For Check Digit (position 9, index 8), must be 0-9 or X
                if (char.IsLetter(candidate[8]) && candidate[8] != 'X')
                {
                    score -= 10.0;
                }
            }

            // Normalize max score to 100
            return score > 100.0 ? 100.0 : score;
        }
    }
}
