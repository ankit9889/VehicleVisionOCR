using VehicleVisionOCR.Domain.NLP.Models;

namespace VehicleVisionOCR.Domain.NLP.Interfaces
{
    public interface ITextNormalizationService
    {
        string Normalize(string rawText, InterpretationProfile config);
    }
}
