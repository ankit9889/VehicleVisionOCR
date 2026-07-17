using System.Collections.Generic;
using VehicleVisionOCR.Benchmarks.Models;

namespace VehicleVisionOCR.Benchmarks.Interfaces
{
    public interface IDatasetRunner
    {
        List<DatasetImage> LoadDataset(string directoryPath);
    }
}
