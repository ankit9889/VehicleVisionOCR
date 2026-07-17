using System.Collections.Generic;
using VehicleVisionOCR.Domain.NLP.Models;

namespace VehicleVisionOCR.Domain.NLP.Interfaces
{
    public interface ISimilarityScoringEngine
    {
        List<SimilarityScore> CalculateScores(string input, List<string> dictionaryCandidates);
    }
}
