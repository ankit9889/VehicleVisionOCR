using System.Collections.Generic;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Domain.Vision.Interfaces
{
    public interface IAdaptiveCropEngine
    {
        List<RegionHypothesis> AdaptRegion(byte[] originalImage, RegionHypothesis baseHypothesis, RegionUnderstandingConfig config);
    }
}
