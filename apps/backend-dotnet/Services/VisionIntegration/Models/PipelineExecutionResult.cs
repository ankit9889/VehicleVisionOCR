using System;

namespace VehicleVisionOCR.Backend.Services.VisionIntegration.Models
{
    public class PipelineExecutionResult
    {
        public string ExtractedVin { get; set; }
        public bool IsSuccessful { get; set; }
        public double ConfidenceScore { get; set; }
        
        // Metadata for UI or Debugging
        public string SourcePipeline { get; set; } // "Legacy" or "Modern"
        public TimeSpan TotalExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }
}
