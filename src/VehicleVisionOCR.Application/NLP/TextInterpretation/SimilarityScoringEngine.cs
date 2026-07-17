using System.Collections.Generic;
using System.Linq;
using VehicleVisionOCR.Domain.NLP.Interfaces;
using VehicleVisionOCR.Domain.NLP.Models;

namespace VehicleVisionOCR.Application.NLP.TextInterpretation
{
    public class SimilarityScoringEngine : ISimilarityScoringEngine
    {
        private readonly IEnumerable<ISimilarityAlgorithm> _algorithms;

        public SimilarityScoringEngine(IEnumerable<ISimilarityAlgorithm> algorithms)
        {
            _algorithms = algorithms;
        }

        public List<SimilarityScore> CalculateScores(string input, List<string> dictionaryCandidates)
        {
            var results = new List<SimilarityScore>();
            
            var levenshtein = _algorithms.FirstOrDefault(a => a.Name == "Damerau-Levenshtein");
            var jaro = _algorithms.FirstOrDefault(a => a.Name == "Jaro-Winkler");

            foreach (var candidate in dictionaryCandidates)
            {
                double levScore = levenshtein != null ? levenshtein.CalculateSimilarity(input, candidate) : 0;
                double jaroScore = jaro != null ? jaro.CalculateSimilarity(input, candidate) : 0;

                // Simple blended scoring approach for now
                double blended = (levScore * 0.5) + (jaroScore * 0.5);

                results.Add(new SimilarityScore
                {
                    CandidateTerm = candidate,
                    LevenshteinDistance = levScore,
                    JaroWinklerScore = jaroScore,
                    FinalBlendedScore = blended,
                    Evidence = $"Levenshtein: {levScore:F2}, JaroWinkler: {jaroScore:F2}"
                });
            }

            return results.OrderByDescending(r => r.FinalBlendedScore).ToList();
        }
    }
}
