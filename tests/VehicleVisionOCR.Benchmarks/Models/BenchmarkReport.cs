using System;
using System.Collections.Generic;

namespace VehicleVisionOCR.Benchmarks.Models
{
    public class BenchmarkReport
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string RunId { get; set; } = Guid.NewGuid().ToString();

        // High Level Accuracy
        public double OverallAccuracy { get; set; }
        public double VinAccuracy { get; set; }
        public double ColorAccuracy { get; set; }
        public double ModelAccuracy { get; set; }
        public double CharacterAccuracy { get; set; }
        
        public int TotalImages { get; set; }
        public int FalsePositives { get; set; }
        public int FalseNegatives { get; set; }

        // Per Phase Metrics
        public double LayoutAccuracy { get; set; }
        public double OcrRawAccuracy { get; set; }
        public double ReasoningAccuracy { get; set; }

        // Performance Metrics
        public TimeSpan TotalExecutionTime { get; set; }
        public TimeSpan AverageTimePerImage { get; set; }
        public double CpuUsagePercent { get; set; }
        public double PeakMemoryMb { get; set; }
        public double AverageMemoryMb { get; set; }
        public int MaxThreadCount { get; set; }

        // Advanced Slices
        public Dictionary<string, double> AccuracyByManufacturer { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> AccuracyByLighting { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> AccuracyByBlur { get; set; } = new Dictionary<string, double>();
        
        public List<FailureAnalysis> Failures { get; set; } = new List<FailureAnalysis>();
        public List<string> ImprovementSuggestions { get; set; } = new List<string>();
    }
}
