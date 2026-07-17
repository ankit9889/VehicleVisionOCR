using System;
using System.Collections.Generic;

namespace VehicleVisionOCR.Benchmarks.Models
{
    public class EvaluationReport
    {
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public string DatasetName { get; set; }
        public int TotalEvaluated { get; set; }
        
        // High-Level Metrics
        public double Top1Accuracy { get; set; }
        public double Top3Accuracy { get; set; }
        public double AverageCharacterErrorRate { get; set; }
        public double AverageWordErrorRate { get; set; }
        
        // Granular Metrics
        public double LayoutSuccessRate { get; set; }
        public double OcrSuccessRate { get; set; }
        public double ReasoningSuccessRate { get; set; }
        
        // Slice Metrics
        public Dictionary<string, double> AccuracyByManufacturer { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> AccuracyByScanner { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> AccuracyByLighting { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> AccuracyByBlurLevel { get; set; } = new Dictionary<string, double>();
        
        // Performance
        public TimeSpan AverageExecutionTimePerImage { get; set; }
        public double PeakMemoryMb { get; set; }
        
        public List<EvaluationResult> FailedEvaluations { get; set; } = new List<EvaluationResult>();
        public Dictionary<string, int> FailureBreakdown { get; set; } = new Dictionary<string, int>();
        
        public List<string> ImprovementSuggestions { get; set; } = new List<string>();
    }
}
