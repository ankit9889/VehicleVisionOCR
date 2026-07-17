using System.Collections.Generic;

namespace VehicleVisionOCR.Domain.Vision.Models
{
    public class ConfidenceScore
    {
        public string Value { get; set; }
        public double Confidence { get; set; }
        
        /// <summary>
        /// Human-readable reasoning for the confidence score (e.g. "Check digit failed", "All 3 OCR passes agreed")
        /// </summary>
        public string Reasoning { get; set; }
        
        /// <summary>
        /// The individual probability components that formed this score.
        /// </summary>
        public Dictionary<string, double> Evidence { get; set; } = new Dictionary<string, double>();
        
        /// <summary>
        /// Alternative candidates and their respective probabilities.
        /// </summary>
        public Dictionary<string, double> AlternativeCandidates { get; set; } = new Dictionary<string, double>();
    }
}
