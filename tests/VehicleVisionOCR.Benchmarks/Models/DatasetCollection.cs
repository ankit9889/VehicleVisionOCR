using System.Collections.Generic;

namespace VehicleVisionOCR.Benchmarks.Models
{
    public class DatasetCollection
    {
        public string DatasetName { get; set; }
        public int TotalImages { get; set; }
        
        // Exposing an IAsyncEnumerable would be optimal for streaming massive datasets,
        // but for framework scaffolding we can use an IEnumerable generator.
        public IEnumerable<GroundTruthRecord> Records { get; set; }
    }
}
