using System.Collections.Generic;
using System.Threading.Tasks;
using VehicleVisionOCR.Benchmarks.Models;

namespace VehicleVisionOCR.Benchmarks.Interfaces
{
    public interface IStressBenchmark
    {
        Task EvaluateStressAsync(List<DatasetImage> dataset, BenchmarkReport report);
    }
}
