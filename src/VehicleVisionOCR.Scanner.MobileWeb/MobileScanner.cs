using System;
using System.Threading.Tasks;
using VehicleVisionOCR.Scanner.Core.Enums;
using VehicleVisionOCR.Scanner.Core.Events;
using VehicleVisionOCR.Scanner.Core.Interfaces;
using VehicleVisionOCR.Scanner.Core.Models;

namespace VehicleVisionOCR.Scanner.MobileWeb
{
    public class MobileScanner : IScanner
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

        public MobileScanner(ScannerInfo info)
        {
            Info = info;
            Capabilities = new ScannerCapabilities
            {
                SupportsImageCapture = true,
                SupportsBarcodeDecoding = true,
                SupportsHardwareTrigger = true // the web button acts as a trigger
            };
        }

        public Task ConnectAsync(ScannerConnectionInfo info)
        {
            State = ScannerState.Connected;
            OnConnected?.Invoke(this, new ScannerConnectedEventArgs { Scanner = Info });
            
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
            return ConnectAsync(new ScannerConnectionInfo { ConnectionType = ConnectionType.TcpIp });
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
            // For a mobile scanner, this method might trigger a push notification to the phone to take a photo.
            // For now, it just waits for the phone to POST the image to the API.
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

        // Method called by the Controller when an image is received from the mobile app
        public void ReceiveImage(byte[] imageBytes)
        {
            if (State != ScannerState.Ready && State != ScannerState.Connected)
            {
                throw new InvalidOperationException("Scanner is not connected or ready.");
            }

            var image = new ScannerImage
            {
                Data = imageBytes,
                Format = ImageFormat.Jpeg, // Assuming the browser sends a JPEG
                Width = 0, // Unknown until processed
                Height = 0,
                CapturedAt = DateTime.UtcNow
            };

            OnImageCaptured?.Invoke(this, new ImageCapturedEventArgs { ScannerId = Info.Id, Image = image });
        }

        public void Dispose()
        {
            DisconnectAsync().Wait();
            GC.SuppressFinalize(this);
        }
    }
}
