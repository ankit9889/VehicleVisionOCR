namespace VehicleVisionOCR.Domain.NLP.Models
{
    public class SimilarityScore
    {
        public string CandidateTerm { get; set; }
        
        public double FinalBlendedScore { get; set; }
        
        public double LevenshteinDistance { get; set; }
        public double JaroWinklerScore { get; set; }
        public double NGramCosineSimilarity { get; set; }
        
        /// <summary>
        /// Explanation of how the final score was derived from the algorithms.
        /// </summary>
        public string Evidence { get; set; }
    }
}
