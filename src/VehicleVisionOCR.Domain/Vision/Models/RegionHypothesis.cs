using System;

namespace VehicleVisionOCR.Domain.Vision.Models
{
    public class RegionHypothesis
    {
        public string HypothesisId { get; set; } = Guid.NewGuid().ToString();
        public string ParentRegionId { get; set; }
        public Enums.ZoneType SemanticTarget { get; set; }
        
        public string Strategy { get; set; } // "Original", "ExpandedTop", "Shrunk", etc.
        
        // Geometry
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public byte[] ImageData { get; set; }
        
        // Evaluation
        public StructuralTelemetry Telemetry { get; set; } = new StructuralTelemetry();
        
        public double FinalScore { get; set; }
        public bool IsRejected { get; set; }
        public string RejectionReason { get; set; }
    }
}
