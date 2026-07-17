namespace VehicleVisionOCR.Benchmarks.Models
{
    public class FailureAnalysis
    {
        public string ImageId { get; set; }
        
        public bool WasOcrWrong { get; set; }
        public bool WasLayoutWrong { get; set; }
        public bool WasInterpretationWrong { get; set; }
        public bool WasVinReasoningWrong { get; set; }
        
        public string Expected { get; set; }
        public string Actual { get; set; }
        public string FailureReason { get; set; }
    }
}
