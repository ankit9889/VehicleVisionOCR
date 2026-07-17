namespace VehicleVisionOCR.Domain.NLP.Interfaces
{
    public interface ISimilarityAlgorithm
    {
        string Name { get; }
        
        /// <summary>
        /// Calculates the similarity between two strings.
        /// Return value is normalized between 0.0 (completely different) and 1.0 (exact match).
        /// </summary>
        double CalculateSimilarity(string source, string target);
    }
}
