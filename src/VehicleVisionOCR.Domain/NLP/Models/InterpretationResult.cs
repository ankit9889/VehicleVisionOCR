using System.Collections.Generic;

namespace VehicleVisionOCR.Domain.NLP.Models
{
    public class InterpretationResult
    {
        public string NormalizedText { get; set; }
        
        public double Confidence { get; set; }
        
        public List<SimilarityScore> Alternatives { get; set; } = new List<SimilarityScore>();
        
        public string Evidence { get; set; }
    }
}
