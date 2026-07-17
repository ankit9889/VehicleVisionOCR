using System.Collections.Generic;

namespace VehicleVisionOCR.Domain.VIN.Models
{
    public class CandidateScore
    {
        public double TotalScore { get; set; }
        
        public double LengthScore { get; set; }
        public double RegexScore { get; set; }
        public double CheckDigitScore { get; set; }
        public double ManufacturerScore { get; set; }
        public double SerialScore { get; set; }
        public double InheritedOcrConfidence { get; set; }
        
        public List<RuleViolation> Violations { get; set; } = new List<RuleViolation>();
        public List<string> RuleBonuses { get; set; } = new List<string>();
    }
}
