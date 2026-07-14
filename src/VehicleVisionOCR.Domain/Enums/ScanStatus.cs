namespace VehicleVisionOCR.Domain.Enums
{
    /// <summary>
    /// Represents the state of a vehicle scan operation.
    /// </summary>
    public enum ScanStatus
    {
        Initiated = 0,
        ImageCaptured = 1,
        ProcessingOcr = 2,
        Completed = 3,
        Failed = 4
    }
}
