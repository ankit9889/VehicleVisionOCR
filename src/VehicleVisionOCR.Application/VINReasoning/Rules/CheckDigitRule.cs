using VehicleVisionOCR.Domain.VIN.Interfaces;
using VehicleVisionOCR.Domain.VIN.Models;

namespace VehicleVisionOCR.Application.VINReasoning.Rules
{
    public class CheckDigitRule : IVinRule
    {
        private readonly IVinCheckDigitEngine _checkDigitEngine;

        public CheckDigitRule(IVinCheckDigitEngine checkDigitEngine)
        {
            _checkDigitEngine = checkDigitEngine;
        }

        public string Name => "ISO 3779 Check Digit";

        public void Evaluate(VinCandidate candidate, VinReasoningConfig config)
        {
            if (string.IsNullOrEmpty(candidate.CandidateString) || (candidate.CandidateString.Length != 17 && candidate.CandidateString.Length != 16 && candidate.CandidateString.Length != 14))
            {
                candidate.Score.CheckDigitScore = 0;
                return; // Length rule will handle the violation
            }

            bool isValid = _checkDigitEngine.Calculate(candidate.CandidateString);

            if (isValid)
            {
                candidate.Score.CheckDigitScore = config.ScoringWeights.CheckDigit;
                candidate.Score.RuleBonuses.Add("Check digit mathematically valid.");
            }
            else
            {
                candidate.Score.CheckDigitScore = 0;
                
                if (config.StrictCheckDigitValidation)
                {
                    candidate.Score.Violations.Add(new RuleViolation
                    {
                        RuleName = Name,
                        Description = "Check digit mathematically invalid.",
                        PointsDeducted = config.ScoringWeights.CheckDigit
                    });
                }
                else
                {
                    // For European vehicles where ISO 3779 isn't strict, we just don't give the points, but we don't log it as a critical violation.
                    candidate.Score.Violations.Add(new RuleViolation
                    {
                        RuleName = Name,
                        Description = "Check digit mathematically invalid (Non-fatal, strict validation disabled).",
                        PointsDeducted = 0
                    });
                }
            }
        }
    }
}
