using System;
using VehicleVisionOCR.Domain.Vision.Interfaces;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Infrastructure.Vision.OpenCV
{
    public class LayoutAnalyzer : ILayoutAnalyzer
    {
        private readonly IRegionDetector _regionDetector;
        private readonly IRegionClassifier _regionClassifier;
        private readonly ICropGenerator _cropGenerator;

        public LayoutAnalyzer(
            IRegionDetector regionDetector,
            IRegionClassifier regionClassifier,
            ICropGenerator cropGenerator)
        {
            _regionDetector = regionDetector;
            _regionClassifier = regionClassifier;
            _cropGenerator = cropGenerator;
        }

        public System.Collections.Generic.IEnumerable<CroppedZone> AnalyzeLayout(byte[] rawImageData)
        {
            var layoutResult = AnalyzeLayoutFull(rawImageData, false);
            return layoutResult.CroppedZones;
        }

        public LayoutResult AnalyzeLayoutFull(byte[] rawImageData, bool renderDebug)
        {
            var result = new LayoutResult();

            // Step 1 & 2 & 3: Normalize, Detect Text Blocks, Detect Barcode
            var regions = _regionDetector.DetectRegions(rawImageData);

            // Step 4 & 5 & 6 & 7: Semantic Classification (Probabilities)
            _regionClassifier.ClassifyRegions(regions);

            result.DetectedRegions = regions;

            // Step 8 & 9 & 10: Generate Cropped Images and Debug Overlay
            var (zones, overlay) = _cropGenerator.GenerateCrops(rawImageData, regions, renderDebug);
            result.CroppedZones = zones;
            result.DebugOverlayImage = overlay;

            return result;
        }
    }
}
