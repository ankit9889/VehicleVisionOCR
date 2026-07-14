namespace VehicleVisionOCR.Domain.Enums
{
    /// <summary>
    /// Represents the severity level or category of a log entry.
    /// </summary>
    public enum LogType
    {
        Info = 0,
        Warning = 1,
        Error = 2,
        Critical = 3,
        ScannerEvent = 4,
        ApiSync = 5
    }
}
