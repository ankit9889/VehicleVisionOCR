using System.Threading.Tasks;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Enums;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Models;

namespace VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces
{
    /// <summary>
    /// Serves as the central entry point for the OCR correction pipeline.
    /// Routes incoming raw OCR data to the appropriate field-specific strategy.
    /// </summary>
    public interface IOcrCorrectionCoordinator
    {
        /// <summary>
        /// Processes the raw OCR output for a specific field by resolving and executing the corresponding strategy.
        /// </summary>
        /// <param name="fieldType">The target field type (e.g., VIN, Color).</param>
        /// <param name="rawText">The raw OCR text.</param>
        /// <param name="ocrConfidence">The OCR engine's confidence score.</param>
        /// <returns>The finalized, validated CorrectionResult.</returns>
        Task<CorrectionResult> ProcessFieldAsync(TargetFieldType fieldType, string rawText, double ocrConfidence);
    }
}
