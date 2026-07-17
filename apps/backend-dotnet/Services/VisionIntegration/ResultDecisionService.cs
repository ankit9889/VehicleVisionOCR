using VehicleVisionOCR.Backend.Services.VisionIntegration.Models;

namespace VehicleVisionOCR.Backend.Services.VisionIntegration
{
    public class ResultDecisionService
    {
        public PipelineExecutionResult Decide(PipelineExecutionResult legacyResult, PipelineExecutionResult modernResult, PipelineMode mode)
        {
            switch (mode)
            {
                case PipelineMode.LegacyOnly:
                case PipelineMode.ShadowMode:
                    return legacyResult;
                    
                case PipelineMode.ModernOnly:
                    // Only return modern if it succeeded, otherwise fail safely to legacy
                    return modernResult.IsSuccessful ? modernResult : legacyResult;
                    
                case PipelineMode.AutomaticSwitch:
                    // For example: Prefer modern, but if modern has confidence < 80%, fall back to legacy.
                    if (modernResult.IsSuccessful && modernResult.ConfidenceScore >= 80.0)
                    {
                        return modernResult;
                    }
                    return legacyResult;
                    
                default:
                    return legacyResult;
            }
        }
    }
}
