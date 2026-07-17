using System.Collections.Generic;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Domain.Vision.Interfaces
{
    public interface IRegionDetector
    {
        /// <summary>
        /// Analyzes a normalized image and detects raw text and barcode blocks without classifying them.
        /// </summary>
        /// <param name="normalizedImage">Preprocessed and deskewed image data (or internal Mat ptr depending on implementation).</param>
        /// <returns>A list of unclassified RegionCandidates with their physical Features populated.</returns>
        List<RegionCandidate> DetectRegions(byte[] normalizedImage);
    }
}
