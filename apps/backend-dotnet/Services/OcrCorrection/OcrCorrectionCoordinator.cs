using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Enums;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Models;

namespace VehicleVisionOCR.Backend.Services.OcrCorrection
{
    /// <summary>
    /// Implementation of the OCR Correction Coordinator.
    /// Utilizes a pre-built dictionary for O(1) strategy resolution and applies structured logging.
    /// </summary>
    public class OcrCorrectionCoordinator : IOcrCorrectionCoordinator
    {
        private readonly Dictionary<TargetFieldType, IOcrCorrectionStrategy> _strategies;
        private readonly ILogger<OcrCorrectionCoordinator> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OcrCorrectionCoordinator"/> class.
        /// </summary>
        /// <param name="strategies">All registered strategies injected via the DI container.</param>
        /// <param name="logger">The structured logger.</param>
        public OcrCorrectionCoordinator(IEnumerable<IOcrCorrectionStrategy> strategies, ILogger<OcrCorrectionCoordinator> logger)
        {
            _logger = logger;
            // O(1) dictionary resolution compiled during startup
            _strategies = strategies.ToDictionary(s => s.FieldType);
        }

        /// <inheritdoc/>
        public async Task<CorrectionResult> ProcessFieldAsync(TargetFieldType fieldType, string rawText, double ocrConfidence)
        {
            if (!_strategies.TryGetValue(fieldType, out var strategy))
            {
                _logger.LogWarning("No OCR correction strategy registered for field type: {FieldType}", fieldType);
                
                return new CorrectionResult
                {
                    OriginalText = rawText,
                    CorrectedText = rawText,
                    IsValid = true, // Default to true to prevent blocking pipeline if no strategy exists
                    WasCorrected = false,
                    FinalScore = ocrConfidence,
                    ConfidenceLevel = ConfidenceLevel.Low,
                    StrategyName = "None (Passthrough)",
                    FailureReason = "No strategy registered for this field."
                };
            }

            try
            {
                var result = await strategy.CorrectAsync(rawText, ocrConfidence);

                // Production-grade structured logging for traceability
                _logger.LogInformation(
                    "OCR Correction [{FieldType}] - Original: '{Original}', Corrected: '{Corrected}', Valid: {IsValid}, Level: {Level}, Score: {Score}%, Rules: {Rules}, Reason: {Reason}",
                    fieldType, 
                    result.OriginalText, 
                    result.CorrectedText, 
                    result.IsValid, 
                    result.ConfidenceLevel,
                    result.FinalScore, 
                    string.Join(", ", result.AppliedRules), 
                    result.FailureReason ?? "None");

                return result;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception during OCR correction for {FieldType}. Returning passthrough result.", fieldType);
                return new CorrectionResult
                {
                    OriginalText = rawText,
                    CorrectedText = rawText,
                    IsValid = true,
                    WasCorrected = false,
                    FinalScore = ocrConfidence,
                    ConfidenceLevel = ConfidenceLevel.Low,
                    StrategyName = "None (Error Fallback)",
                    FailureReason = $"Pipeline error: {ex.Message}"
                };
            }
        }
    }
}
