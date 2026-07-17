using VehicleVisionOCR.Domain.VIN.Interfaces;
using VehicleVisionOCR.Domain.VIN.Models;

namespace VehicleVisionOCR.Application.VINReasoning.Rules
{
    public class LengthRule : IVinRule
    {
        public string Name => "Length Validation";

        public void Evaluate(VinCandidate candidate, VinReasoningConfig config)
        {
            int length = candidate.CandidateString?.Length ?? 0;

            if (length >= 14 && length <= 20)
            {
                candidate.Score.LengthScore = config.ScoringWeights.Length;
                candidate.Score.RuleBonuses.Add($"Length is {length} characters.");
            }
            else
            {
                candidate.Score.LengthScore = 0;
                candidate.Score.Violations.Add(new RuleViolation
                {
                    RuleName = Name,
                    Description = $"Invalid length ({length}). Must be between 14 and 20 characters.",
                    PointsDeducted = config.ScoringWeights.Length
                });
            }
        }
    }
}
