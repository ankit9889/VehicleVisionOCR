using System.Threading;
using System.Threading.Tasks;
using VehicleVisionOCR.Backend.Services.VisionIntegration.Models;

namespace VehicleVisionOCR.Backend.Services.VisionIntegration.Interfaces
{
    public interface IScanProcessingEngine
    {
        Task<ScanProcessingResult> ProcessScanAsync(byte[] scanPayload, CancellationToken cancellationToken = default);
    }
}
