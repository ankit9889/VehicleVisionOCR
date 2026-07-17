using System.Threading.Tasks;
using VehicleVisionOCR.Benchmarks.Models;

namespace VehicleVisionOCR.Benchmarks.Interfaces
{
    public interface IRegressionBenchmark
    {
        Task CompareAgainstBaselineAsync(BenchmarkReport currentReport, string baselineReportPath);
    }
}
