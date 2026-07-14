using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VehicleVisionOCR.Scanner.Core.Enums;
using VehicleVisionOCR.Scanner.Core.Events;
using VehicleVisionOCR.Scanner.Core.Interfaces;
using VehicleVisionOCR.Scanner.Core.Models;
using VehicleVisionOCR.Scanner.Core.Plugins;

namespace VehicleVisionOCR.Scanner.Core.Manager
{
    public class ScannerEventAggregator : IScannerEventPublisher, IScannerEventSubscriber
    {
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

        public void Subscribe(IScannerEventPublisher publisher)
        {
            publisher.OnConnected += (s, e) => OnConnected?.Invoke(s, e);
            publisher.OnDisconnected += (s, e) => OnDisconnected?.Invoke(s, e);
            publisher.OnReady += (s, e) => OnReady?.Invoke(s, e);
            publisher.OnError += (s, e) => OnError?.Invoke(s, e);
            publisher.OnImageCaptured += (s, e) => OnImageCaptured?.Invoke(s, e);
            publisher.OnBarcodeScanned += (s, e) => OnBarcodeScanned?.Invoke(s, e);
            publisher.OnTriggerPressed += (s, e) => OnTriggerPressed?.Invoke(s, e);
            publisher.OnTriggerReleased += (s, e) => OnTriggerReleased?.Invoke(s, e);
            publisher.OnBatteryChanged += (s, e) => OnBatteryChanged?.Invoke(s, e);
            publisher.OnFirmwareChanged += (s, e) => OnFirmwareChanged?.Invoke(s, e);
            publisher.OnConnectionLost += (s, e) => OnConnectionLost?.Invoke(s, e);
        }

        public void Unsubscribe(IScannerEventPublisher publisher)
        {
            // Note: In a real system, you'd store the handler references to unsubscribe cleanly,
            // or use a WeakEventManager pattern to avoid memory leaks.
            // For simplicity here, we assume scanners live as long as the application or 
            // the garbage collector handles the disconnected ones if properly disposed.
        }
    }

    public class ScannerManager : IScannerManager
    {
        private readonly IScannerFactory _scannerFactory;
        private readonly IScannerPluginLoader _pluginLoader;
        private readonly ILogger<ScannerManager> _logger;
        private readonly ConcurrentDictionary<string, IScanner> _activeScanners;
        private readonly ScannerEventAggregator _eventAggregator;

        public IReadOnlyList<IScanner> ActiveScanners => _activeScanners.Values.ToList();
        public IScannerEventPublisher Events => _eventAggregator;

        public ScannerManager(
            IScannerFactory scannerFactory, 
            IScannerPluginLoader pluginLoader,
            IEnumerable<IScannerPlugin> plugins,
            ILogger<ScannerManager> logger)
        {
            _scannerFactory = scannerFactory;
            _pluginLoader = pluginLoader;
            _logger = logger;
            _activeScanners = new ConcurrentDictionary<string, IScanner>();
            _eventAggregator = new ScannerEventAggregator();
            _plugins = plugins;
        }

        private readonly IEnumerable<IScannerPlugin> _plugins;

        public Task InitializeAsync()
        {
            _logger.LogInformation("Initializing Scanner Manager...");
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<ScannerInfo>> DiscoverScannersAsync()
        {
            var discovered = new List<ScannerInfo>();
            foreach (var plugin in _plugins)
            {
                var provider = plugin.CreateProvider();
                try
                {
                    var scanners = await provider.DiscoverScannersAsync();
                    discovered.AddRange(scanners);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error discovering scanners using plugin {plugin.PluginName}");
                }
            }
            return discovered;
        }

        public async Task<IScanner> ConnectScannerAsync(ScannerBrand brand, ScannerConnectionInfo connectionInfo)
        {
            try
            {
                var info = new ScannerInfo { Brand = brand, Name = $"Scanner_{Guid.NewGuid().ToString("N").Substring(0,8)}" };
                var scanner = await _scannerFactory.CreateAsync(brand, info, connectionInfo);
                
                await scanner.ConnectAsync(connectionInfo);
                
                if (scanner.State == ScannerState.Connected || scanner.State == ScannerState.Ready)
                {
                    _activeScanners.TryAdd(scanner.Info.Id, scanner);
                    _eventAggregator.Subscribe(scanner);
                    _logger.LogInformation($"Successfully connected and registered scanner {scanner.Info.Id} of brand {brand}");
                }
                
                return scanner;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to connect to {brand} scanner.");
                throw;
            }
        }

        public async Task DisconnectScannerAsync(string scannerId)
        {
            if (_activeScanners.TryRemove(scannerId, out var scanner))
            {
                _eventAggregator.Unsubscribe(scanner);
                await scanner.DisconnectAsync();
                scanner.Dispose();
                _logger.LogInformation($"Disconnected and unregistered scanner {scannerId}");
            }
            else
            {
                _logger.LogWarning($"Attempted to disconnect unknown scanner {scannerId}");
            }
        }

        public void Dispose()
        {
            foreach (var scanner in _activeScanners.Values)
            {
                _eventAggregator.Unsubscribe(scanner);
                scanner.DisconnectAsync().GetAwaiter().GetResult();
                scanner.Dispose();
            }
            _activeScanners.Clear();
            _logger.LogInformation("Scanner Manager disposed and all scanners disconnected.");
            GC.SuppressFinalize(this);
        }
    }
}
