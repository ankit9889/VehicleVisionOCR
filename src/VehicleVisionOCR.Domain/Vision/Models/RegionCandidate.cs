using VehicleVisionOCR.Domain.Vision.Enums;

namespace VehicleVisionOCR.Domain.Vision.Models
{
    public class RegionCandidate
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public RegionFeatures Features { get; set; } = new RegionFeatures();

        /// <summary>
        /// The Semantic classification of this region (VIN, Color, Barcode, etc.)
        /// </summary>
        public ZoneType AssignedZone { get; set; } = ZoneType.Unknown;

        /// <summary>
        /// Confidence (0.0 to 1.0) that this classification is correct based on features.
        /// </summary>
        public double ClassificationConfidence { get; set; }

        /// <summary>
        /// Human readable reasoning for why it was classified this way.
        /// </summary>
        public string Reasoning { get; set; } = string.Empty;
    }
}
