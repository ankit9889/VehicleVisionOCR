using VehicleVisionOCR.Benchmarks.Models;

namespace VehicleVisionOCR.Benchmarks.Services
{
    public class FailureAnalyzer
    {
        public void Analyze(EvaluationResult result)
        {
            if (result.IsPerfectMatch)
            {
                result.FailureClassification = "None";
                result.FailureReason = "Success";
                return;
            }

            if (!result.IsLayoutSuccessful)
            {
                result.FailureClassification = "Layout Failure";
                result.FailureReason = "The Layout Analyzer failed to isolate the expected geometric region for the text.";
                return;
            }

            if (!result.IsOcrSuccessful)
            {
                result.FailureClassification = "OCR Failure";
                result.FailureReason = $"The fused OCR text fell below acceptable probability thresholds. CER: {result.CharacterErrorRate:P2}";
                return;
            }

            if (!result.IsInterpretationSuccessful)
            {
                result.FailureClassification = "Interpretation Failure";
                result.FailureReason = "The NLP layer incorrectly mutated the text away from the true value.";
                return;
            }

            if (!result.IsReasoningSuccessful)
            {
                result.FailureClassification = "Reasoning Failure";
                result.FailureReason = "The Rule Engine chose a lower-probability candidate over the truth, likely due to flawed scoring weights or incorrect Check Digit mapping.";
                return;
            }

            result.FailureClassification = "Unknown Failure";
            result.FailureReason = "The evaluation failed but passed all phase health checks. Investigate manually.";
        }
    }
}
