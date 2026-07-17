using System.Collections.Generic;
using OpenCvSharp;
using VehicleVisionOCR.Domain.Vision.Interfaces;
using VehicleVisionOCR.Domain.Vision.Models;
using VehicleVisionOCR.Domain.Vision.Enums;

namespace VehicleVisionOCR.Infrastructure.Vision.OpenCV
{
    public class AdaptiveCropEngine : IAdaptiveCropEngine
    {
        public List<RegionHypothesis> AdaptRegion(byte[] originalImage, RegionHypothesis baseHypothesis, RegionUnderstandingConfig config)
        {
            var adaptations = new List<RegionHypothesis>();
            
            // If the structural evaluator found multiple lines in what should be a single-line semantic region
            if (baseHypothesis.Telemetry.LineCount > 1 && baseHypothesis.SemanticTarget == ZoneType.Vin)
            {
                using var src = Cv2.ImDecode(originalImage, ImreadModes.Color);
                if (src.Empty()) return adaptations;
                
                // Adaptive Split (Horizontal split roughly in half, as a simplistic fallback)
                // Real implementation would split based on exact Y-coordinates from Telemetry.
                int splitY = baseHypothesis.Height / 2;
                
                // Top half
                adaptations.Add(CreateHypothesis(src, baseHypothesis, "AdaptiveTopHalf", baseHypothesis.X, baseHypothesis.Y, baseHypothesis.Width, splitY));
                
                // Bottom half
                adaptations.Add(CreateHypothesis(src, baseHypothesis, "AdaptiveBottomHalf", baseHypothesis.X, baseHypothesis.Y + splitY, baseHypothesis.Width, baseHypothesis.Height - splitY));
            }
            
            return adaptations;
        }
        
        private RegionHypothesis CreateHypothesis(Mat src, RegionHypothesis parent, string strategy, int x, int y, int width, int height)
        {
            if (width <= 0 || height <= 0 || x < 0 || y < 0 || x + width > src.Cols || y + height > src.Rows) return null;

            using var croppedMat = new Mat(src, new Rect(x, y, width, height));
            var imageBytes = croppedMat.ImEncode(".png");
            
            return new RegionHypothesis
            {
                ParentRegionId = parent.HypothesisId,
                Strategy = strategy,
                SemanticTarget = parent.SemanticTarget,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                ImageData = imageBytes
            };
        }
    }
}
