using System;

namespace VehicleVisionOCR.Domain.Vision.Models
{
    public class StructuralTelemetry
    {
        public int LineCount { get; set; }
        public int CharacterCount { get; set; }
        public double AverageCharacterHeight { get; set; }
        public double BaselineConsistency { get; set; }
        public double CharacterSpacingUniformity { get; set; }
        public double BorderProximity { get; set; }
        public double OverallLayoutConfidence { get; set; }
        public string RawStructuralText { get; set; }
    }
}
