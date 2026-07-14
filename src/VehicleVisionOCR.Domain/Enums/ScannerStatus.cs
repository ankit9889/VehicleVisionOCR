namespace VehicleVisionOCR.Domain.Enums
{
    /// <summary>
    /// Represents the connection status of a scanner device.
    /// </summary>
    public enum ScannerStatus
    {
        Disconnected = 0,
        Connecting = 1,
        Connected = 2,
        Error = 3
    }
}
