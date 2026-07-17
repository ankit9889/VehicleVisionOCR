using VehicleVisionOCR.Domain.VIN.Interfaces;
using VehicleVisionOCR.Domain.VIN.Models;

namespace VehicleVisionOCR.Application.VINReasoning.Rules
{
    public class WmiValidationRule : IVinRule
    {
        private readonly IWmiKnowledgeProvider _wmiProvider;

        public WmiValidationRule(IWmiKnowledgeProvider wmiProvider)
        {
            _wmiProvider = wmiProvider;
        }

        public string Name => "WMI Registry Validation";

        public void Evaluate(VinCandidate candidate, VinReasoningConfig config)
        {
            if (string.IsNullOrEmpty(candidate.CandidateString) || candidate.CandidateString.Length < 3)
            {
                candidate.Score.ManufacturerScore = 0;
                return;
            }

            string wmi = candidate.CandidateString.Substring(0, 3);
            var manufacturerData = _wmiProvider.GetManufacturerData(wmi);

            if (manufacturerData != null)
            {
                candidate.Score.ManufacturerScore = config.ScoringWeights.Manufacturer;
                candidate.ExtractedManufacturer = manufacturerData;
                candidate.Score.RuleBonuses.Add($"WMI '{wmi}' mapped to {manufacturerData.ManufacturerName} ({manufacturerData.Country}).");
            }
            else
            {
                candidate.Score.ManufacturerScore = 0;
                candidate.Score.Violations.Add(new RuleViolation
                {
                    RuleName = Name,
                    Description = $"WMI '{wmi}' does not exist in manufacturer database.",
                    PointsDeducted = config.ScoringWeights.Manufacturer
                });
            }
        }
    }
}
