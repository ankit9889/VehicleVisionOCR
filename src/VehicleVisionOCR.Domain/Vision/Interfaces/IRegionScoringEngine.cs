using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Domain.Vision.Interfaces
{
    public interface IRegionScoringEngine
    {
        void ScoreHypothesis(RegionHypothesis hypothesis, RegionUnderstandingConfig config);
    }
}
