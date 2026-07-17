using System.Threading;
using System.Threading.Tasks;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Domain.Vision.Interfaces
{
    public interface IRegionUnderstandingEngine
    {
        Task<LayoutResult> AnalyzeLayoutWithUnderstandingAsync(byte[] imageBytes, RegionUnderstandingConfig config, CancellationToken cancellationToken = default);
    }
}
