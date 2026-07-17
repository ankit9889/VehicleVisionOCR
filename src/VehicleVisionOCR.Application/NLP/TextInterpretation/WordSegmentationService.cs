using System.Collections.Generic;
using System.Linq;
using VehicleVisionOCR.Domain.NLP.Interfaces;

namespace VehicleVisionOCR.Application.NLP.TextInterpretation
{
    public class WordSegmentationService : IWordSegmentationService
    {
        public List<string> Segment(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();

            // Basic space separation for now. 
            // In a more advanced implementation, this could use Viterbi algorithm
            // to segment unspaced words (e.g. "PEARLWHITE" -> "PEARL", "WHITE") based on dictionary frequencies.
            var tokens = text.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries)
                             .Select(t => t.Trim())
                             .Where(t => t.Length > 0)
                             .ToList();

            return tokens;
        }
    }
}
