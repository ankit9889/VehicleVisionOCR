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
        private readonly OcrCorrectionOptions _options;

        public VinScoringService(Microsoft.Extensions.Options.IOptions<OcrCorrectionOptions> options)
        {
            _options = options?.Value ?? new OcrCorrectionOptions();
        }

        [GeneratedRegex("^[A-HJ-NPR-Z0-9]{14,20}$")]
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
            string pattern = $"^[A-HJ-NPR-Z0-9]{{{_options.MinVinLength},{_options.MaxVinLength}}}$";
            if (Regex.IsMatch(candidate, pattern))
            {
                score += 30.0;
            }

            // 3. WMI Verification Bonus (Weight: +5%)
            if (candidate.Length >= 3 && knownWmis != null && knownWmis.Contains(candidate.Substring(0, 3)))
            {
                score += 5.0;
            }

            // 4. ISO 3779 Check Digit (Weight: 30%)
            if (candidate.Length == 17 || candidate.Length == 16)
            {
                bool isCheckDigitValid = VinCheckDigitCalculator.Validate(candidate);
                if (isCheckDigitValid)
                {
                    score += 30.0;
                }
                else if (candidate.Length == 17)
                {
                    // Severe penalty if check digit fails but string looks otherwise perfect
                    score -= 20.0;
                }
            }

            // 5. Positional Constraints Penalty
            if (candidate.Length == 17 || candidate.Length == 16)
            {
                // I, O, Q are never allowed in a VIN
                for (int i = 0; i < candidate.Length; i++)
                {
                    if (candidate[i] == 'I' || candidate[i] == 'O' || candidate[i] == 'Q')
                    {
                        score -= 15.0;
                    }
                }

                // For 17-char VINs, last 4-6 must be numeric.
                // For 16-char VINs, we assume the last 5-6 are the serial number.
                int visStartIndex = candidate.Length == 17 ? 11 : 10;
                for (int i = visStartIndex; i < candidate.Length; i++)
                {
                    if (char.IsLetter(candidate[i]))
                    {
                        score -= (i >= visStartIndex + 2) ? 10.0 : 5.0;
                    }
                }

                // For Check Digit (position 9, index 8), if mandatory, it must be 0-9 or X
                char wmiRegion = candidate[0];
                bool isCheckDigitMandatory = (wmiRegion == '1' || wmiRegion == '2' || wmiRegion == '3' || 
                                             wmiRegion == '4' || wmiRegion == '5' || wmiRegion == 'L') && candidate.Length == 17;

                if (isCheckDigitMandatory)
                {
                    if (char.IsLetter(candidate[8]) && candidate[8] != 'X')
                    {
                        score -= 15.0; // Penalty if 9th pos is not digit or X
                    }
                }
                else if (candidate.Length == 17 && char.IsLetter(candidate[8]) && candidate[8] != 'X')
                {
                    // Soft penalty: Even if not strictly enforced by region, 17-char VINs rarely use letters here.
                    // This mathematically breaks ties in favor of valid 16-char Asian VINs.
                    score -= 5.0;
                }

            }

            // Return raw score to allow mathematical tie-breakers (like WMI) to work instead of capping at 100.
            return score;
        }
    }
}
