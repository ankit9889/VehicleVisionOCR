using System;
using System.Collections.Generic;

namespace VehicleVisionOCR.Domain.Vision.Models
{
    public class OcrObservation
    {
        public string PassId { get; set; } = Guid.NewGuid().ToString();
        public string RawText { get; set; }
        public double AverageConfidence { get; set; }
        
        public string PreprocessingMethod { get; set; }
        public int PageSegmentationMode { get; set; }
        public double Scale { get; set; }
        
        public TimeSpan ExecutionTime { get; set; }
        
        public List<CharacterEvidence> Characters { get; set; } = new List<CharacterEvidence>();
    }
}
