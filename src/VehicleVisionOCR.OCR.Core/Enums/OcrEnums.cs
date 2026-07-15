namespace VehicleVisionOCR.OCR.Core.Enums
{
    public enum OcrEngineType
    {
        Unknown,
        Tesseract,
        Azure,
        GoogleVision,
        AwsTextract,
        EasyOcr,
        PositionBased,
        Custom
    }

    public enum OcrStatus
    {
        Pending,
        Processing,
        Success,
        Partial,
        Failed
    }
}
