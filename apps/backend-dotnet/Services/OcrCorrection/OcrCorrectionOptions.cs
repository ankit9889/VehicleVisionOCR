namespace VehicleVisionOCR.Backend.Services.OcrCorrection
{
    /// <summary>
    /// Configuration options defining thresholds and cache durations for the OCR Correction Pipeline.
    /// Configured via appsettings.json.
    /// </summary>
    public class OcrCorrectionOptions
    {
        /// <summary>
        /// The configuration section key in appsettings.json.
        /// </summary>
        public const string Section = "OcrCorrection";

        /// <summary>
        /// The absolute minimum composite score a VIN candidate must achieve to be marked as IsValid = true.
        /// Default is 60.0.
        /// </summary>
        public double MinVinScoreThreshold { get; set; } = 60.0;

        /// <summary>
        /// The absolute minimum composite score a Color candidate must achieve to be marked as IsValid = true.
        /// Default is 65.0.
        /// </summary>
        public double MinColorScoreThreshold { get; set; } = 65.0;

        /// <summary>
        /// The duration in minutes to hold database lookups (e.g., WMI Prefixes, Active Colors) in IMemoryCache before requiring a fresh pull.
        /// Default is 60 minutes.
        /// </summary>
        public int CacheExpirationMinutes { get; set; } = 60;
    }
}
