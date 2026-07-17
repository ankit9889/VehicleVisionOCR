using System;

namespace VehicleVisionOCR.Backend.Services.VisionIntegration.Models
{
    public class ComparisonResult
    {
        public Guid LogId { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public string LegacyVin { get; set; }
        public string ModernVin { get; set; }
        
        public bool IsMatch { get; set; }
        
        public TimeSpan LegacyExecutionTime { get; set; }
        public TimeSpan ModernExecutionTime { get; set; }
        
        public double ModernConfidence { get; set; }
        public string ModernFailureReason { get; set; }
    }
}
