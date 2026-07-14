using System;
using VehicleVisionOCR.Scanner.Core.Enums;

namespace VehicleVisionOCR.Scanner.Core.Models
{
    public class ScannerInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string FirmwareVersion { get; set; } = string.Empty;
        public ScannerBrand Brand { get; set; } = ScannerBrand.Unknown;
        public ScannerType Type { get; set; } = ScannerType.Unknown;
    }

    public class ScannerConfiguration
    {
        public bool EnableImageCapture { get; set; } = true;
        public bool EnableIllumination { get; set; } = true;
        public bool EnableAiming { get; set; } = true;
        public ScanMode Mode { get; set; } = ScanMode.Single;
    }

    public class ScannerCapabilities
    {
        public bool SupportsImageCapture { get; set; }
        public bool SupportsBarcodeDecoding { get; set; }
        public bool SupportsOcr { get; set; }
        public bool SupportsHardwareTrigger { get; set; }
    }

    public class ScannerHealth
    {
        public bool IsHealthy { get; set; }
        public int? BatteryLevel { get; set; }
        public string? DiagnosticsMessage { get; set; }
        public DateTime LastCheckedAt { get; set; }
    }

    public class ScannerConnectionInfo
    {
        public ConnectionType ConnectionType { get; set; }
        public string PortOrAddress { get; set; } = string.Empty;
        public bool IsSecure { get; set; }
    }

    public class ScannerImage
    {
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public ImageFormat Format { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public DateTime CapturedAt { get; set; }
    }

    public class BarcodeData
    {
        public string RawData { get; set; } = string.Empty;
        public BarcodeFormat Format { get; set; }
        public DateTime ScannedAt { get; set; }
    }

    public class ScanSession
    {
        public Guid SessionId { get; set; } = Guid.NewGuid();
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int TotalScans { get; set; }
    }

    public class ScanStatistics
    {
        public int SuccessfulScans { get; set; }
        public int FailedScans { get; set; }
        public TimeSpan AverageScanTime { get; set; }
    }
}
