using System.Collections.Generic;

namespace VehicleVisionOCR.Domain.Vision.Models
{
    public class LayoutResult
    {
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        
        public double OverallConfidence { get; set; }
        
        public List<RegionCandidate> DetectedRegions { get; set; } = new List<RegionCandidate>();
        
        public List<CroppedZone> CroppedZones { get; set; } = new List<CroppedZone>();
        
        /// <summary>
        /// Optional debug overlays (e.g. bounding boxes drawn on original image)
        /// </summary>
        public byte[] DebugOverlayImage { get; set; }
    }
}
