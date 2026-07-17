using System.Threading.Tasks;
using VehicleVisionOCR.Benchmarks.Models;

namespace VehicleVisionOCR.Benchmarks.Interfaces
{
    public interface IDatasetManager
    {
        Task<DatasetCollection> LoadDatasetAsync(string path);
    }
}
