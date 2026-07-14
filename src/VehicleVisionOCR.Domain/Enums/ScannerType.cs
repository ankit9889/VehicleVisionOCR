namespace VehicleVisionOCR.Domain.Enums
{
    /// <summary>
    /// Represents the type of scanner device used.
    /// </summary>
    public enum ScannerType
    {
        /// <summary>
        /// Unknown or unconfigured scanner type.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Windows-connected Zebra scanner via CoreScanner SDK.
        /// </summary>
        ZebraWindows = 1,

        /// <summary>
        /// Android-based Zebra scanner using DataWedge intents.
        /// </summary>
        ZebraDataWedge = 2,

        /// <summary>
        /// Virtual scanner used for testing.
        /// </summary>
        Virtual = 99
    }
}
