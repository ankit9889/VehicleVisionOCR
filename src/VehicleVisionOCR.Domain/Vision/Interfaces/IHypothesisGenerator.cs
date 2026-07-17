using System.Collections.Generic;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Domain.Vision.Interfaces
{
    public interface IHypothesisGenerator
    {
        List<RegionHypothesis> GenerateHypotheses(byte[] originalImage, RegionCandidate baseRegion, Enums.ZoneType targetType, RegionUnderstandingConfig config);
    }
}
