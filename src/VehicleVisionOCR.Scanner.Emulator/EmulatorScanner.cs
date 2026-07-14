using System;
using System.Threading.Tasks;
using VehicleVisionOCR.Scanner.Core.Enums;
using VehicleVisionOCR.Scanner.Core.Events;
using VehicleVisionOCR.Scanner.Core.Interfaces;
using VehicleVisionOCR.Scanner.Core.Models;

using VehicleVisionOCR.Scanner.Core.Enums;

namespace VehicleVisionOCR.Scanner.Emulator
{
    public class EmulatorScanner : IScanner
    {
        public ScannerInfo Info { get; }
        public ScannerState State { get; private set; } = ScannerState.Disconnected;
        public ScannerCapabilities Capabilities { get; }

        public event EventHandler<ScannerConnectedEventArgs>? OnConnected;
        public event EventHandler<ScannerDisconnectedEventArgs>? OnDisconnected;
        public event EventHandler<ScannerReadyEventArgs>? OnReady;
        public event EventHandler<ScannerErrorEventArgs>? OnError;
        public event EventHandler<ImageCapturedEventArgs>? OnImageCaptured;
        public event EventHandler<BarcodeScannedEventArgs>? OnBarcodeScanned;
        public event EventHandler<TriggerPressedEventArgs>? OnTriggerPressed;
        public event EventHandler<TriggerReleasedEventArgs>? OnTriggerReleased;
        public event EventHandler<BatteryChangedEventArgs>? OnBatteryChanged;
        public event EventHandler<FirmwareChangedEventArgs>? OnFirmwareChanged;
        public event EventHandler<ConnectionLostEventArgs>? OnConnectionLost;

        public EmulatorScanner(ScannerInfo info)
        {
            Info = info;
            Capabilities = new ScannerCapabilities
            {
                SupportsImageCapture = true,
                SupportsBarcodeDecoding = true,
                SupportsHardwareTrigger = true
            };
        }

        public Task ConnectAsync(ScannerConnectionInfo info)
        {
            State = ScannerState.Connected;
            OnConnected?.Invoke(this, new ScannerConnectedEventArgs { Scanner = Info });
            
            // Simulate ready shortly after connecting
            Task.Delay(500).ContinueWith(_ => 
            {
                State = ScannerState.Ready;
                OnReady?.Invoke(this, new ScannerReadyEventArgs { ScannerId = Info.Id });
            });

            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            State = ScannerState.Disconnected;
            OnDisconnected?.Invoke(this, new ScannerDisconnectedEventArgs { ScannerId = Info.Id });
            return Task.CompletedTask;
        }

        public Task ReconnectAsync()
        {
            return ConnectAsync(new ScannerConnectionInfo { ConnectionType = ConnectionType.USB });
        }

        public Task ApplyConfigurationAsync(ScannerConfiguration config)
        {
            return Task.CompletedTask;
        }

        public Task<ScannerConfiguration> GetCurrentConfigurationAsync()
        {
            return Task.FromResult(new ScannerConfiguration());
        }

        public Task<ScannerHealth> GetHealthStatusAsync()
        {
            return Task.FromResult(new ScannerHealth 
            { 
                BatteryLevel = 100, 
                IsHealthy = true 
            });
        }

        public Task CaptureImageAsync()
        {
            var path = @"C:\Users\ASUS\.gemini\antigravity\scratch\VehicleVisionOCR\mock_scan.png";

            if (System.IO.File.Exists(path))
            {
                var bytes = System.IO.File.ReadAllBytes(path);
                var image = new ScannerImage
                {
                    Data = bytes,
                    Format = ImageFormat.Png,
                    Width = 800,
                    Height = 400,
                    CapturedAt = DateTime.UtcNow
                };
                OnImageCaptured?.Invoke(this, new ImageCapturedEventArgs { ScannerId = Info.Id, Image = image });
            }
            
            return Task.CompletedTask;
        }

        public Task TriggerDecodeAsync()
        {
            return Task.CompletedTask;
        }

        public Task SoundBeepAsync(BeepPattern pattern)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            DisconnectAsync().Wait();
            GC.SuppressFinalize(this);
        }
    }
}
