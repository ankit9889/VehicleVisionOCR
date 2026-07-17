using System.Threading.Tasks;
using VehicleVisionOCR.Domain.Vision.Models;
using VehicleVisionOCR.Domain.NLP.Models;

namespace VehicleVisionOCR.Domain.NLP.Interfaces
{
    public interface ITextInterpretationEngine
    {
        Task<InterpretationResult> InterpretAsync(FusedStringCandidate input, InterpretationProfile config);
    }
}
