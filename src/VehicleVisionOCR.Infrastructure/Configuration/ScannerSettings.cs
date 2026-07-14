namespace VehicleVisionOCR.Infrastructure.Configuration
{
    public class ScannerSettings
    {
        public const string SectionName = "Scanner";
        
        public string DefaultPlugin { get; set; } = string.Empty;
        public bool AutoConnect { get; set; } = true;
    }
}
