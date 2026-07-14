using System;
using VehicleVisionOCR.Scanner.Core.Enums;
using VehicleVisionOCR.Scanner.Core.Models;

namespace VehicleVisionOCR.Scanner.Core.Events
{
    public class ScannerConnectedEventArgs : EventArgs { public ScannerInfo Scanner { get; set; } = new(); }
    public class ScannerDisconnectedEventArgs : EventArgs { public string ScannerId { get; set; } = string.Empty; }
    public class ScannerReadyEventArgs : EventArgs { public string ScannerId { get; set; } = string.Empty; }
    public class ScannerErrorEventArgs : EventArgs { public string ScannerId { get; set; } = string.Empty; public string ErrorMessage { get; set; } = string.Empty; public Exception? Exception { get; set; } }
    
    public class ImageCapturedEventArgs : EventArgs 
    { 
        public string ScannerId { get; set; } = string.Empty; 
        public ScannerImage Image { get; set; } = new(); 
    }
    
    public class BarcodeScannedEventArgs : EventArgs 
    { 
        public string ScannerId { get; set; } = string.Empty; 
        public BarcodeData Barcode { get; set; } = new(); 
    }
    
    public class TriggerPressedEventArgs : EventArgs { public string ScannerId { get; set; } = string.Empty; }
    public class TriggerReleasedEventArgs : EventArgs { public string ScannerId { get; set; } = string.Empty; }
    public class BatteryChangedEventArgs : EventArgs { public string ScannerId { get; set; } = string.Empty; public int BatteryLevel { get; set; } }
    public class FirmwareChangedEventArgs : EventArgs { public string ScannerId { get; set; } = string.Empty; public string NewFirmware { get; set; } = string.Empty; }
    public class ConnectionLostEventArgs : EventArgs { public string ScannerId { get; set; } = string.Empty; public string Reason { get; set; } = string.Empty; }
}
