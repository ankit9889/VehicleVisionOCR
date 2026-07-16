namespace VehicleVisionOCR.Backend.Services.OcrCorrection.Enums
{
    /// <summary>
    /// Represents the level of confidence the system has in the final corrected OCR output.
    /// Used by the UI and manual review workflows to flag potentially incorrect data.
    /// </summary>
    public enum ConfidenceLevel
    {
        /// <summary>
        /// The correction score was critically low. Manual intervention is highly recommended.
        /// </summary>
        Low,

        /// <summary>
        /// The correction score met minimum thresholds but lacked absolute certainty.
        /// </summary>
        Medium,

        /// <summary>
        /// The correction passed strict mathematical or database verifications. High probability of accuracy.
        /// </summary>
        High
    }
}
