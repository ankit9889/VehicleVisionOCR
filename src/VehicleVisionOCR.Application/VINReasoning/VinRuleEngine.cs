using System.Collections.Generic;
using VehicleVisionOCR.Domain.VIN.Interfaces;
using VehicleVisionOCR.Domain.VIN.Models;

namespace VehicleVisionOCR.Application.VINReasoning
{
    public class VinRuleEngine
    {
        private readonly IEnumerable<IVinRule> _rules;

        public VinRuleEngine(IEnumerable<IVinRule> rules)
        {
            _rules = rules;
        }

        public void Evaluate(VinCandidate candidate, VinReasoningConfig config)
        {
            foreach (var rule in _rules)
            {
                rule.Evaluate(candidate, config);
            }

            CalculateTotalScore(candidate, config);
        }

        private void CalculateTotalScore(VinCandidate candidate, VinReasoningConfig config)
        {
            // Add positive attributes
            double score = 0;
            score += candidate.Score.LengthScore;
            score += candidate.Score.RegexScore;
            score += candidate.Score.CheckDigitScore;
            score += candidate.Score.ManufacturerScore;
            score += candidate.Score.SerialScore;
            score += candidate.Score.InheritedOcrConfidence;

            // Subtract penalties (e.g. heavy deductions for illegal chars)
            foreach (var violation in candidate.Score.Violations)
            {
                score -= violation.PointsDeducted;
            }

            candidate.Score.TotalScore = score;
        }
    }
}
