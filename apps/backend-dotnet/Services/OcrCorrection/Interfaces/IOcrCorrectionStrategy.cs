using System.Threading.Tasks;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Enums;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Models;

namespace VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces
{
    /// <summary>
    /// Defines the contract for an OCR correction strategy.
    /// Each specific domain field (e.g., VIN, Color) must implement this interface.
    /// </summary>
    public interface IOcrCorrectionStrategy
    {
        /// <summary>
        /// Identifies the specific field type this strategy is designed to handle.
        /// </summary>
        TargetFieldType FieldType { get; }

        /// <summary>
        /// Analyzes the raw OCR text and confidence, applies domain-specific rules,
        /// and returns a structured, scored correction result.
        /// </summary>
        /// <param name="rawText">The original text extracted by the OCR engine.</param>
        /// <param name="ocrConfidence">The raw confidence score (0-100) provided by the OCR engine.</param>
        /// <returns>A rich CorrectionResult domain object.</returns>
        Task<CorrectionResult> CorrectAsync(string rawText, double ocrConfidence);
    }
}
