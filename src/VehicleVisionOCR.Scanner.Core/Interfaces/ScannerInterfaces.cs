using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VehicleVisionOCR.Scanner.Core.Enums;
using VehicleVisionOCR.Scanner.Core.Models;
using VehicleVisionOCR.Scanner.Core.Events;

namespace VehicleVisionOCR.Scanner.Core.Interfaces
{
    // Interface Segregation Principle: Split responsibilities

    public interface IScannerPlugin
    {
        string PluginId { get; }
        string PluginName { get; }
        string Version { get; }
        ScannerBrand SupportedBrand { get; }
        IScannerProvider CreateProvider();
    }

    public interface IScannerProvider
    {
        Task<IEnumerable<ScannerInfo>> DiscoverScannersAsync();
        Task<IScanner> CreateScannerAsync(ScannerInfo info, ScannerConnectionInfo connection);
    }

    public interface IScannerDiscovery
    {
        Task<IEnumerable<ScannerInfo>> SearchAsync(ConnectionType? type = null);
    }

    public interface IScannerConnection : IDisposable
    {
        ScannerState State { get; }
        Task ConnectAsync(ScannerConnectionInfo info);
        Task DisconnectAsync();
        Task ReconnectAsync();
    }

    public interface IScannerConfigurationManager
    {
        Task ApplyConfigurationAsync(ScannerConfiguration config);
        Task<ScannerConfiguration> GetCurrentConfigurationAsync();
    }

    public interface IScannerHealthMonitor
    {
        Task<ScannerHealth> GetHealthStatusAsync();
    }

    public interface IScannerCapability
    {
        ScannerCapabilities Capabilities { get; }
    }

    public interface IScannerImageCapture
    {
        Task CaptureImageAsync();
    }

    public interface IScannerBarcodeReader
    {
        Task TriggerDecodeAsync();
    }

    public interface IScannerFeedback
    {
        Task SoundBeepAsync(BeepPattern pattern);
    }

    public interface IScannerEventPublisher
    {
        event EventHandler<ScannerConnectedEventArgs> OnConnected;
        event EventHandler<ScannerDisconnectedEventArgs> OnDisconnected;
        event EventHandler<ScannerReadyEventArgs> OnReady;
        event EventHandler<ScannerErrorEventArgs> OnError;
        event EventHandler<ImageCapturedEventArgs> OnImageCaptured;
        event EventHandler<BarcodeScannedEventArgs> OnBarcodeScanned;
        event EventHandler<TriggerPressedEventArgs> OnTriggerPressed;
        event EventHandler<TriggerReleasedEventArgs> OnTriggerReleased;
        event EventHandler<BatteryChangedEventArgs> OnBatteryChanged;
        event EventHandler<FirmwareChangedEventArgs> OnFirmwareChanged;
        event EventHandler<ConnectionLostEventArgs> OnConnectionLost;
    }

    public interface IScannerEventSubscriber
    {
        void Subscribe(IScannerEventPublisher publisher);
        void Unsubscribe(IScannerEventPublisher publisher);
    }

    // Facade/Composite interface
    public interface IScanner : 
        IScannerConnection, 
        IScannerConfigurationManager, 
        IScannerHealthMonitor, 
        IScannerCapability, 
        IScannerImageCapture, 
        IScannerBarcodeReader, 
        IScannerFeedback,
        IScannerEventPublisher
    {
        ScannerInfo Info { get; }
    }

    public interface IScannerFactory
    {
        Task<IScanner> CreateAsync(ScannerBrand brand, ScannerInfo info, ScannerConnectionInfo connectionInfo);
    }

    public interface IScannerManager : IDisposable
    {
        IReadOnlyList<IScanner> ActiveScanners { get; }
        Task InitializeAsync();
        Task<IScanner> ConnectScannerAsync(ScannerBrand brand, ScannerConnectionInfo connectionInfo);
        Task DisconnectScannerAsync(string scannerId);
        Task<IEnumerable<ScannerInfo>> DiscoverScannersAsync();
        
        // Expose aggregated events from all scanners
        IScannerEventPublisher Events { get; }
    }
}
