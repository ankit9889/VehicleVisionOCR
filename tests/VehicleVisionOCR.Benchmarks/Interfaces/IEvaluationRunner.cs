using System.Threading.Tasks;
using VehicleVisionOCR.Benchmarks.Models;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Benchmarks.Interfaces
{
    public interface IEvaluationRunner
    {
        Task<EvaluationReport> RunEvaluationAsync(DatasetCollection dataset, OcrProfileConfig config);
    }
}
