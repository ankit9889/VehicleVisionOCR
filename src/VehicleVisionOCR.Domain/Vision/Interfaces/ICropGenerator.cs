using System.Collections.Generic;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Domain.Vision.Interfaces
{
    public interface ICropGenerator
    {
        /// <summary>
        /// Slices the original image into individual CroppedZones based on classified RegionCandidates.
        /// Can also optionally produce a debug overlay image if requested.
        /// </summary>
        (List<CroppedZone> zones, byte[] debugOverlay) GenerateCrops(byte[] originalImage, List<RegionCandidate> regions, bool renderDebug);
    }
}
