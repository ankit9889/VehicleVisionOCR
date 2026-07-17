using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Domain.Vision.Interfaces
{
    public interface ISemanticRegionValidator
    {
        bool ValidateHypothesis(RegionHypothesis hypothesis, Enums.ZoneType targetType);
    }
}
