using System;

namespace VehicleVisionOCR.Domain.Vision.Models
{
    public class RegionUnderstandingConfig
    {
        public int MaxHypothesesPerRegion { get; set; } = 5;
        public int PixelExpansionStep { get; set; } = 5;
        public bool EnableAdaptiveCropping { get; set; } = true;
        public double MinimumVinScoreThreshold { get; set; } = 0.65;
        public double MinimumColorScoreThreshold { get; set; } = 0.50;
        public bool OutputDebugArtifacts { get; set; } = true;
    }
}
