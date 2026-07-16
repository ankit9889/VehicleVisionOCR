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
        /// Evaluates OCR confidence, length/pattern heuristics, confusion probability, and ISO 3779 mathematical correctness.
        /// </summary>
        /// <param name="candidate">The generated candidate object containing substitutions and rules.</param>
        /// <param name="rawOcrText">The unedited raw text emitted by the OCR engine.</param>
        /// <param name="ocrConfidence">The raw confidence provided by the OCR engine.</param>
        /// <param name="knownWmis">A list of known valid WMIs from the database.</param>
        /// <returns>A composite score from 0.0 to 100.0.</returns>
        double ScoreCandidate(Models.CandidateScore candidate, string rawOcrText, double ocrConfidence, List<string> knownWmis);
    }
}
