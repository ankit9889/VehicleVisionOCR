using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using VehicleVisionOCR.Scanner.Core.Enums;
using VehicleVisionOCR.Scanner.Core.Events;
using VehicleVisionOCR.Scanner.Core.Interfaces;
using VehicleVisionOCR.Scanner.Core.Models;
using VehicleVisionOCR.Scanner.ZebraWindows.Exceptions;

namespace VehicleVisionOCR.Scanner.ZebraWindows
{
    public class ZebraScannerConnection : IScanner
    {
        private readonly ILogger _logger;
        private readonly object? _coreScanner;
        private readonly int _scannerId;

        private Delegate? _barcodeEventDelegate;
        private Delegate? _imageEventDelegate;
        private Delegate? _pnpEventDelegate;
        private EventInfo? _barcodeEventInfo;
        private EventInfo? _imageEventInfo;
        private EventInfo? _pnpEventInfo;

        public ScannerInfo Info { get; }
        public ScannerState State { get; private set; } = ScannerState.Disconnected;
        public ScannerCapabilities Capabilities { get; } = new ScannerCapabilities();

#pragma warning disable CS0067
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
#pragma warning restore CS0067

        public ZebraScannerConnection(ScannerInfo info, object? coreScanner, ILogger logger)
        {
            Info = info;
            _coreScanner = coreScanner;
            _logger = logger;

            if (int.TryParse(info.Id, out int parsedId))
            {
                _scannerId = parsedId;
            }
            else
            {
                _scannerId = 1; // Default
            }

            Capabilities.SupportsBarcodeDecoding = true;
            Capabilities.SupportsImageCapture = true; 
            Capabilities.SupportsHardwareTrigger = true;
        }

        public Task ConnectAsync(ScannerConnectionInfo info)
        {
            return Task.Run(() =>
            {
                try
                {
                    State = ScannerState.Connecting;
                    
                    if (_coreScanner != null)
                    {
                        Type t = _coreScanner.GetType();
                        
                        // Reflection-based event subscription
                        _barcodeEventInfo = t.GetEvent("BarcodeEvent");
                        if (_barcodeEventInfo != null && _barcodeEventInfo.EventHandlerType != null)
                        {
                            _barcodeEventDelegate = Delegate.CreateDelegate(_barcodeEventInfo.EventHandlerType, this, GetType().GetMethod("CoreScanner_BarcodeEvent", BindingFlags.NonPublic | BindingFlags.Instance)!);
                            _barcodeEventInfo.AddEventHandler(_coreScanner, _barcodeEventDelegate);
                        }

                        _imageEventInfo = t.GetEvent("ImageEvent");
                        if (_imageEventInfo != null && _imageEventInfo.EventHandlerType != null)
                        {
                            _imageEventDelegate = Delegate.CreateDelegate(_imageEventInfo.EventHandlerType, this, GetType().GetMethod("CoreScanner_ImageEvent", BindingFlags.NonPublic | BindingFlags.Instance)!);
                            _imageEventInfo.AddEventHandler(_coreScanner, _imageEventDelegate);
                        }

                        _pnpEventInfo = t.GetEvent("PNPEvent");
                        if (_pnpEventInfo != null && _pnpEventInfo.EventHandlerType != null)
                        {
                            _pnpEventDelegate = Delegate.CreateDelegate(_pnpEventInfo.EventHandlerType, this, GetType().GetMethod("CoreScanner_PNPEvent", BindingFlags.NonPublic | BindingFlags.Instance)!);
                            _pnpEventInfo.AddEventHandler(_coreScanner, _pnpEventDelegate);
                        }
                    }

                    string inXml = $"<inArgs><scannerID>{_scannerId}</scannerID></inArgs>";
                    string outXml = string.Empty;
                    int status = 0;

                    if (_coreScanner != null)
                    {
                        dynamic dynScanner = _coreScanner;
                        dynScanner.ExecCommand(2014, ref inXml, out outXml, out status);
                    }

                    State = ScannerState.Connected;
                    OnConnected?.Invoke(this, new ScannerConnectedEventArgs { Scanner = Info });
                    
                    State = ScannerState.Ready;
                    OnReady?.Invoke(this, new ScannerReadyEventArgs { ScannerId = Info.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to scanner {ScannerId}", _scannerId);
                    State = ScannerState.Error;
                    OnError?.Invoke(this, new ScannerErrorEventArgs { ScannerId = Info.Id, ErrorMessage = ex.Message, Exception = ex });
                    throw new ZebraScannerException("Failed to register scanner events.", ex);
                }
            });
        }

        private void UnregisterEvents()
        {
            try
            {
                if (_coreScanner != null)
                {
                    if (_barcodeEventInfo != null && _barcodeEventDelegate != null)
                        _barcodeEventInfo.RemoveEventHandler(_coreScanner, _barcodeEventDelegate);
                    
                    if (_imageEventInfo != null && _imageEventDelegate != null)
                        _imageEventInfo.RemoveEventHandler(_coreScanner, _imageEventDelegate);
                    
                    if (_pnpEventInfo != null && _pnpEventDelegate != null)
                        _pnpEventInfo.RemoveEventHandler(_coreScanner, _pnpEventDelegate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering events.");
            }
        }

        public Task DisconnectAsync()
        {
            return Task.Run(() =>
            {
                UnregisterEvents();

                State = ScannerState.Disconnected;
                OnDisconnected?.Invoke(this, new ScannerDisconnectedEventArgs { ScannerId = Info.Id });
            });
        }

        public async Task ReconnectAsync()
        {
            await DisconnectAsync();
            await ConnectAsync(new ScannerConnectionInfo()); 
        }

        public void Dispose()
        {
            DisconnectAsync().GetAwaiter().GetResult();
            GC.SuppressFinalize(this);
        }

        public Task ApplyConfigurationAsync(ScannerConfiguration config)
        {
            return Task.Run(() =>
            {
                _logger.LogInformation($"Applying configuration to Zebra Scanner {_scannerId}");
            });
        }

        public Task<ScannerConfiguration> GetCurrentConfigurationAsync()
        {
            return Task.FromResult(new ScannerConfiguration());
        }

        public Task<ScannerHealth> GetHealthStatusAsync()
        {
            return Task.FromResult(new ScannerHealth
            {
                IsHealthy = State == ScannerState.Ready,
                LastCheckedAt = DateTime.UtcNow
            });
        }

        public Task CaptureImageAsync()
        {
            return Task.Run(() =>
            {
                string inXml = $"<inArgs><scannerID>{_scannerId}</scannerID></inArgs>";
                string outXml = string.Empty;
                int status = 0;
                
                if (_coreScanner != null)
                {
                    dynamic dynScanner = _coreScanner;
                    dynScanner.ExecCommand(2011, ref inXml, out outXml, out status);
                }
                
                if (status != 0)
                    throw new ZebraScannerException($"Failed to trigger image capture. Status: {status}");
            });
        }

        public Task TriggerDecodeAsync()
        {
             return Task.Run(() =>
            {
                string inXml = $"<inArgs><scannerID>{_scannerId}</scannerID></inArgs>";
                string outXml = string.Empty;
                int status = 0;
                
                if (_coreScanner != null)
                {
                    dynamic dynScanner = _coreScanner;
                    dynScanner.ExecCommand(2011, ref inXml, out outXml, out status);
                }
            });
        }

        public Task SoundBeepAsync(BeepPattern pattern)
        {
            return Task.Run(() =>
            {
                int beepCode = pattern switch
                {
                    BeepPattern.Success => 1,
                    BeepPattern.Warning => 12,
                    BeepPattern.Error => 13,
                    _ => 1
                };

                string inXml = $"<inArgs><scannerID>{_scannerId}</scannerID><cmdArgs><arg-int>{beepCode}</arg-int></cmdArgs></inArgs>";
                string outXml = string.Empty;
                int status = 0;
                
                if (_coreScanner != null)
                {
                    dynamic dynScanner = _coreScanner;
                    dynScanner.ExecCommand(6000, ref inXml, out outXml, out status);
                }
                
                if (status != 0)
                    _logger.LogWarning($"Failed to trigger beep on scanner {_scannerId}. Status: {status}");
            });
        }

        private void CoreScanner_BarcodeEvent(short eventType, ref string pscanData)
        {
            try
            {
                var xml = XDocument.Parse(pscanData);
                var datalabel = xml.Descendants("datalabel").FirstOrDefault()?.Value ?? string.Empty;
                
                var barcodeData = new BarcodeData
                {
                    RawData = datalabel,
                    Format = BarcodeFormat.Unknown, 
                    ScannedAt = DateTime.UtcNow
                };

                OnBarcodeScanned?.Invoke(this, new BarcodeScannedEventArgs { ScannerId = Info.Id, Barcode = barcodeData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing barcode event data.");
            }
        }

        private void CoreScanner_ImageEvent(short eventType, int size, short imageFormat, ref object sfsaImageData, ref string pScannerData)
        {
            try
            {
                byte[]? imageData = sfsaImageData as byte[];
                
                if (imageData != null)
                {
                    var image = new ScannerImage
                    {
                        Data = imageData,
                        CapturedAt = DateTime.UtcNow,
                        Format = imageFormat == 1 ? ImageFormat.Jpeg : ImageFormat.Bmp
                    };

                    OnImageCaptured?.Invoke(this, new ImageCapturedEventArgs { ScannerId = Info.Id, Image = image });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing image event data.");
            }
        }

        private void CoreScanner_PNPEvent(short eventType, ref string ppnpData)
        {
            if (eventType == 0) 
            {
            }
            else if (eventType == 1) 
            {
                State = ScannerState.Disconnected;
                OnConnectionLost?.Invoke(this, new ConnectionLostEventArgs { ScannerId = Info.Id, Reason = "Device detached." });
            }
        }
    }
}
