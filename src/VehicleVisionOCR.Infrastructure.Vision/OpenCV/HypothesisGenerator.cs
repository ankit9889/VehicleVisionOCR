using System.Collections.Generic;
using OpenCvSharp;
using VehicleVisionOCR.Domain.Vision.Interfaces;
using VehicleVisionOCR.Domain.Vision.Models;
using VehicleVisionOCR.Domain.Vision.Enums;

namespace VehicleVisionOCR.Infrastructure.Vision.OpenCV
{
    public class HypothesisGenerator : IHypothesisGenerator
    {
        public List<RegionHypothesis> GenerateHypotheses(byte[] originalImage, RegionCandidate baseRegion, ZoneType targetType, RegionUnderstandingConfig config)
        {
            var hypotheses = new List<RegionHypothesis>();
            
            using var src = Cv2.ImDecode(originalImage, ImreadModes.Color);
            if (src.Empty()) return hypotheses;
            
            // 1. Original Hypothesis
            hypotheses.Add(CreateHypothesis(src, baseRegion, "Original", 0, 0, 0, 0, targetType));
            
            // 2. Expanded Overall (Add Padding)
            int p = config.PixelExpansionStep;
            hypotheses.Add(CreateHypothesis(src, baseRegion, "ExpandedOverall", -p, -p, p * 2, p * 2, targetType));
            
            // 3. Shrunk Overall (Remove Padding)
            hypotheses.Add(CreateHypothesis(src, baseRegion, "ShrunkOverall", p, p, -p * 2, -p * 2, targetType));
            
            // 4. Expanded Vertically
            hypotheses.Add(CreateHypothesis(src, baseRegion, "ExpandedVertical", 0, -p, 0, p * 2, targetType));
            
            return hypotheses;
        }

        private RegionHypothesis CreateHypothesis(Mat src, RegionCandidate baseRegion, string strategy, int dx, int dy, int dw, int dh, ZoneType targetType)
        {
            int x = System.Math.Max(0, baseRegion.X + dx);
            int y = System.Math.Max(0, baseRegion.Y + dy);
            int width = System.Math.Min(src.Cols - x, baseRegion.Width + dw);
            int height = System.Math.Min(src.Rows - y, baseRegion.Height + dh);
            
            if (width <= 0 || height <= 0) return null;

            using var croppedMat = new Mat(src, new Rect(x, y, width, height));
            var imageBytes = croppedMat.ImEncode(".png");
            
            return new RegionHypothesis
            {
                ParentRegionId = baseRegion.GetHashCode().ToString(),
                Strategy = strategy,
                SemanticTarget = targetType,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                ImageData = imageBytes
            };
        }
    }
}
