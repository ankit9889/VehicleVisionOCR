namespace VehicleVisionOCR.Infrastructure.Configuration
{
    public class OCRSettings
    {
        public const string SectionName = "OCR";
        
        public string DefaultEngine { get; set; } = string.Empty;
        public string TessDataPath { get; set; } = string.Empty;
        public double MinimumConfidenceThreshold { get; set; } = 70.0;
    }
}
