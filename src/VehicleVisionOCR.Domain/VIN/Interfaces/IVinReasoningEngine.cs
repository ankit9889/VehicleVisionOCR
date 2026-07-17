using System.Threading.Tasks;
using VehicleVisionOCR.Domain.NLP.Models;
using VehicleVisionOCR.Domain.VIN.Models;

namespace VehicleVisionOCR.Domain.VIN.Interfaces
{
    public interface IVinReasoningEngine
    {
        Task<VinReasoningResult> ReasonAsync(InterpretationResult input, VinReasoningConfig config, System.Threading.CancellationToken cancellationToken = default);
    }
}
