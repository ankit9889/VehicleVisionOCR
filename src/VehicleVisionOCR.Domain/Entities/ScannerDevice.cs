using System;
using VehicleVisionOCR.Domain.Common;
using VehicleVisionOCR.Domain.Enums;

namespace VehicleVisionOCR.Domain.Entities
{
    /// <summary>
    /// Represents a hardware scanner device connected to the system.
    /// </summary>
    public class ScannerDevice : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }
        public string? FirmwareVersion { get; set; }
        public ScannerType Type { get; set; } = ScannerType.Unknown;
        public ScannerStatus Status { get; set; } = ScannerStatus.Disconnected;
        public int? BatteryLevel { get; set; }
        public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    }
}
