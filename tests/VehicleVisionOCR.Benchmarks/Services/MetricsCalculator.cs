using System;
using System.Collections.Generic;
using System.Linq;
using VehicleVisionOCR.Benchmarks.Models;

namespace VehicleVisionOCR.Benchmarks.Services
{
    public class MetricsCalculator
    {
        public void Calculate(EvaluationReport report, List<EvaluationResult> results)
        {
            if (results == null || !results.Any()) return;

            int total = results.Count;
            report.TotalEvaluated = total;

            int top1Hits = results.Count(r => r.IsPerfectMatch);
            int layoutSuccesses = results.Count(r => r.IsLayoutSuccessful);
            int ocrSuccesses = results.Count(r => r.IsOcrSuccessful);
            int reasoningSuccesses = results.Count(r => r.IsReasoningSuccessful);

            report.Top1Accuracy = (double)top1Hits / total;
            report.LayoutSuccessRate = (double)layoutSuccesses / total;
            report.OcrSuccessRate = (double)ocrSuccesses / total;
            report.ReasoningSuccessRate = (double)reasoningSuccesses / total;

            report.AverageCharacterErrorRate = results.Average(r => r.CharacterErrorRate);
            
            // Slice Aggregations
            report.AccuracyByManufacturer = CalculateSliceAccuracy(results, r => r.GroundTruth.ExpectedManufacturer);
            report.AccuracyByLighting = CalculateSliceAccuracy(results, r => r.GroundTruth.Lighting);
            report.AccuracyByScanner = CalculateSliceAccuracy(results, r => r.GroundTruth.ScannerModel);
            report.AccuracyByBlurLevel = CalculateSliceAccuracy(results, r => r.GroundTruth.ImageQuality);
            
            report.FailedEvaluations = results.Where(r => !r.IsPerfectMatch).ToList();
            
            report.FailureBreakdown = report.FailedEvaluations
                .GroupBy(r => r.FailureClassification ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());
        }

        private Dictionary<string, double> CalculateSliceAccuracy(List<EvaluationResult> results, Func<EvaluationResult, string> selector)
        {
            return results
                .GroupBy(selector)
                .ToDictionary(
                    g => g.Key ?? "Unknown",
                    g => (double)g.Count(r => r.IsPerfectMatch) / g.Count()
                );
        }

        public double CalculateCER(string actual, string expected)
        {
            if (string.IsNullOrEmpty(actual) && string.IsNullOrEmpty(expected)) return 0.0;
            if (string.IsNullOrEmpty(actual)) return 1.0;
            if (string.IsNullOrEmpty(expected)) return 1.0;

            int distance = LevenshteinDistance(actual, expected);
            return (double)distance / System.Math.Max(actual.Length, expected.Length);
        }

        private int LevenshteinDistance(string source, string target)
        {
            int n = source.Length;
            int m = target.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (int i = 0; i <= n; d[i, 0] = i++) { }
            for (int j = 0; j <= m; d[0, j] = j++) { }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                    d[i, j] = System.Math.Min(
                        System.Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }
    }
}
