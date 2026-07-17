using System;
using System.Collections.Generic;

namespace VehicleVisionOCR.Backend.Services.VisionIntegration.Models
{
    public class ScanProcessingResult
    {
        public bool IsSuccess { get; set; }
        
        public string ExtractedVin { get; set; }
        public string ExtractedRegistrationNumber { get; set; }
        public string ExtractedColor { get; set; }
        
        public double Confidence { get; set; }
        public string PipelineUsed { get; set; }
        public TimeSpan TotalExecutionTime { get; set; }
        public string DiagnosticMessage { get; set; }
        
        public string RawText { get; set; }
        public Dictionary<string, string> ExtractedFields { get; set; } = new Dictionary<string, string>();
    }
}
