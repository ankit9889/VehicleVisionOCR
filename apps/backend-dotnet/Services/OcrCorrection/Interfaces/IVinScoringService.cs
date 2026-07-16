using System.Collections.Generic;

namespace VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces
{
    /// <summary>
    /// Calculates a composite confidence score for a VIN candidate based on multiple heuristics.
    /// </summary>
    public interface IVinScoringService
    {
        /// <summary>
        /// Scores the provided candidate from 0 to 100.
        /// Evaluates OCR confidence, length/pattern heuristics, and ISO 3779 mathematical correctness.
        /// </summary>
        /// <param name="candidate">The generated 17-character VIN candidate.</param>
        /// <param name="rawOcrText">The unedited raw text emitted by the OCR engine.</param>
        /// <param name="ocrConfidence">The raw confidence provided by the OCR engine.</param>
        /// <param name="knownWmis">A list of known valid WMIs from the database.</param>
        /// <returns>A composite score from 0.0 to 100.0.</returns>
        double ScoreCandidate(string candidate, string rawOcrText, double ocrConfidence, List<string> knownWmis);
    }
}
