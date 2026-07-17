using System;
using System.Collections.Generic;

namespace VehicleVisionOCR.Domain.VIN.Models
{
    public class VinReasoningResult
    {
        public bool IsValid { get; set; }
        
        public string WinningVin { get; set; }
        public double FinalConfidenceScore { get; set; }
        
        public VinCandidate WinningCandidateDetails { get; set; }
        
        public List<VinCandidate> AlternativeCandidates { get; set; } = new List<VinCandidate>();
        
        public TimeSpan ExecutionTime { get; set; }
    }
}
