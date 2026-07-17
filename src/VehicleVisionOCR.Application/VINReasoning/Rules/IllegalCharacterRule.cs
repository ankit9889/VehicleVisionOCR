using VehicleVisionOCR.Domain.VIN.Interfaces;
using VehicleVisionOCR.Domain.VIN.Models;

namespace VehicleVisionOCR.Application.VINReasoning.Rules
{
    public class IllegalCharacterRule : IVinRule
    {
        public string Name => "Illegal Character Check";

        public void Evaluate(VinCandidate candidate, VinReasoningConfig config)
        {
            if (string.IsNullOrEmpty(candidate.CandidateString)) return;

            string invalidCharsFound = "";
            foreach (char c in candidate.CandidateString)
            {
                if (c == 'I' || c == 'O' || c == 'Q')
                {
                    if (!invalidCharsFound.Contains(c))
                        invalidCharsFound += c;
                }
            }

            if (invalidCharsFound.Length > 0)
            {
                // Penalize heavily. We don't deduct from a specific bucket, 
                // we'll record a massive violation that drops the TotalScore.
                candidate.Score.Violations.Add(new RuleViolation
                {
                    RuleName = Name,
                    Description = $"Contains illegal characters: {invalidCharsFound}",
                    PointsDeducted = 50.0 // Heavy penalty
                });
            }
        }
    }
}
