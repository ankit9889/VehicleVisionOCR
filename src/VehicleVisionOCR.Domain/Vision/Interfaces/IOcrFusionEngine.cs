using System.Threading.Tasks;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Domain.Vision.Interfaces
{
    public interface IOcrFusionEngine
    {
        Task<FusionResult> ProcessZoneAsync(CroppedZone zone, OcrProfileConfig config);
    }
}
