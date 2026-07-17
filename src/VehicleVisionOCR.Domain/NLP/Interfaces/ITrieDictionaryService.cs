using System.Collections.Generic;

namespace VehicleVisionOCR.Domain.NLP.Interfaces
{
    public interface ITrieDictionaryService
    {
        void LoadDictionary(List<string> terms);
        List<string> FindClosestMatches(string term, int maxDistance);
    }
}
