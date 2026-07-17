namespace VehicleVisionOCR.Benchmarks.Models
{
    public class DatasetImage
    {
        public string ImageId { get; set; }
        public string FilePath { get; set; }
        
        // Ground Truth
        public string ExpectedVin { get; set; }
        public string ExpectedColor { get; set; }
        public string ExpectedModel { get; set; }
        public string ExpectedBarcode { get; set; }
        public string ExpectedManufacturer { get; set; }
        
        // Metadata for categorical reporting
        public string LightingCondition { get; set; } // e.g. "Daylight", "Glare", "Shadow"
        public string BlurLevel { get; set; } // e.g. "None", "Low", "High"
    }
}
