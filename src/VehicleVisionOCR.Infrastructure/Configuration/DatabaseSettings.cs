namespace VehicleVisionOCR.Infrastructure.Configuration
{
    public class DatabaseSettings
    {
        public const string SectionName = "Database";
        
        public string ConnectionString { get; set; } = string.Empty;
        public bool EnableSensitiveDataLogging { get; set; }
        public int CommandTimeoutSeconds { get; set; } = 30;
    }
}
