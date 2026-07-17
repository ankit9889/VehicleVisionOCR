using System.Collections.Generic;
using System.Linq;
using VehicleVisionOCR.Domain.NLP.Interfaces;

namespace VehicleVisionOCR.Application.NLP.TextInterpretation
{
    public class TrieDictionaryService : ITrieDictionaryService
    {
        private List<string> _inMemoryDictionary = new List<string>();

        public void LoadDictionary(List<string> terms)
        {
            if (terms != null)
            {
                // In a production environment with millions of words, this would populate a Trie or BK-Tree.
                _inMemoryDictionary = terms.Distinct().ToList();
            }
        }

        public List<string> FindClosestMatches(string term, int maxDistance)
        {
            // Fallback for smaller dictionaries: simply return the whole dictionary to be evaluated 
            // by the SimilarityScoringEngine, which is fast enough for <1000 items.
            // If the dictionary exceeds this, a BK-Tree radius search should be implemented here.
            return _inMemoryDictionary;
        }
    }
}
