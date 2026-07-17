using System.Threading.Tasks;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Domain.Vision.Interfaces
{
    public interface IRegionStructuralEvaluator
    {
        Task<StructuralTelemetry> EvaluateStructureAsync(RegionHypothesis hypothesis, OcrProfileConfig ocrConfig);
    }
}
