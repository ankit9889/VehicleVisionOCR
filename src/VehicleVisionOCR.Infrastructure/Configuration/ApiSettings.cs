namespace VehicleVisionOCR.Infrastructure.Configuration
{
    public class ApiSettings
    {
        public const string SectionName = "Api";
        
        public string BaseUrl { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
        public int RetryCount { get; set; } = 3;
    }
}
