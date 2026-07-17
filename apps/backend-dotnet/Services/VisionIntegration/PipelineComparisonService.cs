using VehicleVisionOCR.Backend.Services.VisionIntegration.Models;

namespace VehicleVisionOCR.Backend.Services.VisionIntegration
{
    public class PipelineComparisonService
    {
        public ComparisonResult Compare(PipelineExecutionResult legacyResult, PipelineExecutionResult modernResult)
        {
            var comparison = new ComparisonResult
            {
                LegacyVin = legacyResult?.ExtractedVin,
                ModernVin = modernResult?.ExtractedVin,
                LegacyExecutionTime = legacyResult?.TotalExecutionTime ?? System.TimeSpan.Zero,
                ModernExecutionTime = modernResult?.TotalExecutionTime ?? System.TimeSpan.Zero,
                ModernConfidence = modernResult?.ConfidenceScore ?? 0,
                ModernFailureReason = modernResult?.ErrorMessage
            };

            if (legacyResult != null && modernResult != null && legacyResult.IsSuccessful && modernResult.IsSuccessful)
            {
                comparison.IsMatch = string.Equals(legacyResult.ExtractedVin, modernResult.ExtractedVin, System.StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                comparison.IsMatch = false;
            }

            return comparison;
        }
    }
}
