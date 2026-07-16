namespace VehicleVisionOCR.Backend.Services.OcrCorrection.Enums
{
    /// <summary>
    /// Identifies the specific domain field being targeted by the OCR extraction process.
    /// Used by the Coordinator to dynamically resolve the correct strategy.
    /// </summary>
    public enum TargetFieldType
    {
        /// <summary>
        /// Vehicle Identification Number (ISO 3779 compliant).
        /// </summary>
        VIN,

        /// <summary>
        /// The exterior color of the vehicle.
        /// </summary>
        Color
    }
}
