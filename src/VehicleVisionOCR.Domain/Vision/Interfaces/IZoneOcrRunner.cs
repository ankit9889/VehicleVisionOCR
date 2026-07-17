using System.Collections.Generic;
using System.Threading.Tasks;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Domain.Vision.Interfaces
{
    public interface IZoneOcrRunner
    {
        Task<List<OcrObservation>> RunOcrPassesAsync(byte[] image, OcrProfileConfig config);
    }
}
