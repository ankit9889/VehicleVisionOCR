using System.Collections.Generic;
using System.Threading.Tasks;
using VehicleVisionOCR.Benchmarks.Models;

namespace VehicleVisionOCR.Benchmarks.Interfaces
{
    public interface IAccuracyBenchmark
    {
        Task EvaluateAccuracyAsync(List<DatasetImage> dataset, BenchmarkReport report);
    }
}
