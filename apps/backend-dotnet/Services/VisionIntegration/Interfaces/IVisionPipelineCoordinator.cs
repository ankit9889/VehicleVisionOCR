using System.Threading;
using System.Threading.Tasks;
using VehicleVisionOCR.Backend.Services.VisionIntegration.Models;

namespace VehicleVisionOCR.Backend.Services.VisionIntegration.Interfaces
{
    public interface IVisionPipelineCoordinator
    {
        Task<PipelineExecutionResult> ProcessImageAsync(byte[] imageBytes, PipelineMode mode, CancellationToken cancellationToken = default);
    }
}
