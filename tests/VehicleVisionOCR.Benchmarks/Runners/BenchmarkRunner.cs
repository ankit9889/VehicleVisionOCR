using System.Collections.Generic;
using System.Threading.Tasks;
using VehicleVisionOCR.Benchmarks.Interfaces;
using VehicleVisionOCR.Benchmarks.Models;

namespace VehicleVisionOCR.Benchmarks.Runners
{
    public class BenchmarkRunner : IBenchmarkRunner
    {
        private readonly IAccuracyBenchmark _accuracyBenchmark;
        private readonly IPerformanceBenchmark _performanceBenchmark;
        private readonly IMemoryBenchmark _memoryBenchmark;
        private readonly IStressBenchmark _stressBenchmark;

        public BenchmarkRunner(
            IAccuracyBenchmark accuracyBenchmark,
            IPerformanceBenchmark performanceBenchmark,
            IMemoryBenchmark memoryBenchmark,
            IStressBenchmark stressBenchmark)
        {
            _accuracyBenchmark = accuracyBenchmark;
            _performanceBenchmark = performanceBenchmark;
            _memoryBenchmark = memoryBenchmark;
            _stressBenchmark = stressBenchmark;
        }

        public async Task<BenchmarkReport> RunSuiteAsync(List<DatasetImage> dataset)
        {
            var report = new BenchmarkReport
            {
                TotalImages = dataset.Count
            };

            // 1. Evaluate pure accuracy (False Positives, Accuracy across layout, OCR, NLP, Reasoning)
            await _accuracyBenchmark.EvaluateAccuracyAsync(dataset, report);

            // 2. Evaluate performance (Execution time, CPU spikes)
            await _performanceBenchmark.EvaluatePerformanceAsync(dataset, report);

            // 3. Evaluate memory (Peak RAM, allocations per run)
            await _memoryBenchmark.EvaluateMemoryAsync(dataset, report);

            // 4. Stress tests (Parallel massive loads)
            await _stressBenchmark.EvaluateStressAsync(dataset, report);

            // Derive overall metrics from the populated report data
            report.OverallAccuracy = (report.LayoutAccuracy + report.OcrRawAccuracy + report.ReasoningAccuracy) / 3.0;

            return report;
        }
    }
}
