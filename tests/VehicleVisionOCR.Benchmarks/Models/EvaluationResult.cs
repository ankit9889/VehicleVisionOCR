using System;
using System.Collections.Generic;
using VehicleVisionOCR.Domain.VIN.Models;

namespace VehicleVisionOCR.Benchmarks.Models
{
    public class EvaluationResult
    {
        public GroundTruthRecord GroundTruth { get; set; }
        
        // Pipeline Outcomes
        public bool IsLayoutSuccessful { get; set; }
        public bool IsOcrSuccessful { get; set; }
        public bool IsInterpretationSuccessful { get; set; }
        public bool IsReasoningSuccessful { get; set; }
        
        // Output Data
        public string ExtractedVin { get; set; }
        public VinReasoningResult ReasoningData { get; set; }
        
        // Metrics
        public bool IsPerfectMatch { get; set; }
        public double CharacterErrorRate { get; set; }
        
        // Failure Data
        public string FailureClassification { get; set; }
        public string FailureReason { get; set; }
        
        public TimeSpan TotalExecutionTime { get; set; }
    }
}
