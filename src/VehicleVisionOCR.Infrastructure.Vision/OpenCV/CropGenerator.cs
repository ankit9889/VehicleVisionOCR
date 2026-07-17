using System.Collections.Generic;
using OpenCvSharp;
using VehicleVisionOCR.Domain.Vision.Interfaces;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Infrastructure.Vision.OpenCV
{
    public class CropGenerator : ICropGenerator
    {
        public (List<CroppedZone> zones, byte[] debugOverlay) GenerateCrops(byte[] originalImage, List<RegionCandidate> regions, bool renderDebug)
        {
            var zones = new List<CroppedZone>();
            byte[] debugBytes = null;

            using var src = Cv2.ImDecode(originalImage, ImreadModes.Color);
            if (src.Empty()) return (zones, debugBytes);

            foreach (var region in regions)
            {
                // Ensure boundaries are safe
                int x = System.Math.Max(0, region.X);
                int y = System.Math.Max(0, region.Y);
                int width = System.Math.Min(src.Cols - x, region.Width);
                int height = System.Math.Min(src.Rows - y, region.Height);

                var rect = new Rect(x, y, width, height);
                if (rect.Width <= 0 || rect.Height <= 0) continue;

                using var croppedMat = new Mat(src, rect);
                var imageBytes = croppedMat.ImEncode(".png");

                zones.Add(new CroppedZone(
                    region.AssignedZone, 
                    x, y, width, height, 
                    imageBytes, 
                    region.ClassificationConfidence
                ));

                if (renderDebug)
                {
                    // Draw bounding box
                    Cv2.Rectangle(src, rect, Scalar.Green, 2);
                    
                    // Draw label
                    string label = $"{region.AssignedZone} ({(region.ClassificationConfidence*100):0}%)";
                    Cv2.PutText(src, label, new Point(x, y - 5), HersheyFonts.HersheySimplex, 0.7, Scalar.Red, 2);

                    // Save debug crop
                    var debugDir = System.IO.Path.Combine(System.Environment.CurrentDirectory, "debug");
                    if (!System.IO.Directory.Exists(debugDir)) System.IO.Directory.CreateDirectory(debugDir);
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(debugDir, $"{region.AssignedZone.ToString().ToLower()}.png"), imageBytes);
                }
            }

            if (renderDebug)
            {
                debugBytes = src.ImEncode(".jpg");
                var debugDir = System.IO.Path.Combine(System.Environment.CurrentDirectory, "debug");
                if (!System.IO.Directory.Exists(debugDir)) System.IO.Directory.CreateDirectory(debugDir);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(debugDir, "debug_overlay.jpg"), debugBytes);
            }

            return (zones, debugBytes);
        }
    }
}
