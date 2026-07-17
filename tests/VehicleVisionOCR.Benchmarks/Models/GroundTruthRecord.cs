namespace VehicleVisionOCR.Benchmarks.Models
{
    public class GroundTruthRecord
    {
        public string ImageId { get; set; }
        public string ImagePath { get; set; }
        
        // Expected Data
        public string ExpectedVin { get; set; }
        public string ExpectedColor { get; set; }
        public string ExpectedModel { get; set; }
        public string ExpectedManufacturer { get; set; }
        public string ExpectedBarcode { get; set; }
        
        // Metadata Attributes
        public string ImageQuality { get; set; } = "High";
        public string Lighting { get; set; } = "Daylight";
        public string Camera { get; set; } = "Unknown";
        public string ScannerModel { get; set; } = "Unknown";
        public int ExpectedOcrDifficulty { get; set; } = 1;
    }
}
