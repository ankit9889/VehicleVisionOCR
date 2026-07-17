using System.Collections.Generic;

namespace VehicleVisionOCR.Domain.Vision.Models
{
    public class FusedStringCandidate
    {
        public string Text { get; set; }
        public double AggregateConfidence { get; set; }
        
        /// <summary>
        /// Breakdown of confidence per character position.
        /// </summary>
        public List<ConfidenceScore> CharacterConfidences { get; set; } = new List<ConfidenceScore>();
    }
}
