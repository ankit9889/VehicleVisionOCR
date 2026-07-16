using System.Collections.Generic;

namespace VehicleVisionOCR.Backend.Services.OcrCorrection.Models
{
    /// <summary>
    /// Represents an intermediate potential correction candidate generated during the OCR pipeline.
    /// Used for scoring against alternative variations.
    /// </summary>
    public class CandidateScore
    {
        /// <summary>
        /// The proposed candidate string.
        /// </summary>
        public string Candidate { get; set; } = string.Empty;

        /// <summary>
        /// The list of normalizations or fuzzy matches applied to generate this candidate.
        /// </summary>
        public List<string> Rules { get; set; } = new();

        /// <summary>
        /// The intermediate score assigned to this candidate before final selection.
        /// </summary>
        public double Score { get; set; }
    }
}
