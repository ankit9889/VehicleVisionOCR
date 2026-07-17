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

            if (length == 17)
            {
                candidate.Score.LengthScore = config.ScoringWeights.Length;
                candidate.Score.RuleBonuses.Add("Length is exactly 17 characters.");
            }
            else if (length == 16 && config.Allow16CharAsianChassis)
            {
                // Give partial or full credit depending on business requirements. Giving full here for now.
                candidate.Score.LengthScore = config.ScoringWeights.Length;
                candidate.Score.RuleBonuses.Add("Length is 16 characters (Asian chassis allowed).");
            }
            else
            {
                candidate.Score.LengthScore = 0;
                candidate.Score.Violations.Add(new RuleViolation
                {
                    RuleName = Name,
                    Description = $"Invalid length ({length}). Must be 17 (or 16 if Asian chassis allowed).",
                    PointsDeducted = config.ScoringWeights.Length
                });
            }
        }
    }
}
