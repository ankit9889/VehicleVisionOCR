namespace VehicleVisionOCR.Domain.Enums
{
    /// <summary>
    /// Represents the status of the OCR extraction process.
    /// </summary>
    public enum OCRStatus
    {
        Pending = 0,
        Processing = 1,
        Success = 2,
        LowConfidence = 3,
        Failed = 4
    }
}
