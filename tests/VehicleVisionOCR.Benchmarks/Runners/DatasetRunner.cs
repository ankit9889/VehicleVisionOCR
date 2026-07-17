using System.Collections.Generic;
using System.IO;
using VehicleVisionOCR.Benchmarks.Interfaces;
using VehicleVisionOCR.Benchmarks.Models;

namespace VehicleVisionOCR.Benchmarks.Runners
{
    public class DatasetRunner : IDatasetRunner
    {
        public List<DatasetImage> LoadDataset(string directoryPath)
        {
            var dataset = new List<DatasetImage>();
            
            // In a real scenario, this would parse a ground-truth CSV or JSON file mapping images to expectations.
            // Mocking the behavior for the framework skeleton.
            if (!Directory.Exists(directoryPath))
            {
                return dataset;
            }

            var files = Directory.GetFiles(directoryPath, "*.jpg");
            foreach(var file in files)
            {
                dataset.Add(new DatasetImage
                {
                    ImageId = Path.GetFileNameWithoutExtension(file),
                    FilePath = file,
                    ExpectedVin = "MOCKED_VIN_FROM_CSV",
                    ExpectedManufacturer = "Mocked_Manufacturer"
                });
            }

            return dataset;
        }
    }
}
