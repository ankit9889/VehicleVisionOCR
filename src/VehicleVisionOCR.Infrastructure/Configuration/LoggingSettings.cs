namespace VehicleVisionOCR.Infrastructure.Configuration
{
    public class LoggingSettings
    {
        public const string SectionName = "Logging";
        
        public string LogDirectory { get; set; } = string.Empty;
        public int RetainedFileCountLimit { get; set; } = 30;
        public string MinimumLevel { get; set; } = "Information";
    }
}
