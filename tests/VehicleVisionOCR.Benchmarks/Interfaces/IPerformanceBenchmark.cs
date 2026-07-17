using System.Collections.Generic;
using System.Threading.Tasks;
using VehicleVisionOCR.Benchmarks.Models;

namespace VehicleVisionOCR.Benchmarks.Interfaces
{
    public interface IPerformanceBenchmark
    {
        Task EvaluatePerformanceAsync(List<DatasetImage> dataset, BenchmarkReport report);
    }
}
