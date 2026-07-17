using System.Collections.Generic;
using System.Threading.Tasks;
using VehicleVisionOCR.Benchmarks.Models;

namespace VehicleVisionOCR.Benchmarks.Interfaces
{
    public interface IBenchmarkRunner
    {
        Task<BenchmarkReport> RunSuiteAsync(List<DatasetImage> dataset);
    }
}
