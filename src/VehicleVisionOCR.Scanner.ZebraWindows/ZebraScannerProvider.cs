using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using VehicleVisionOCR.Scanner.Core.Enums;
using VehicleVisionOCR.Scanner.Core.Interfaces;
using VehicleVisionOCR.Scanner.Core.Models;
using VehicleVisionOCR.Scanner.ZebraWindows.Exceptions;


namespace VehicleVisionOCR.Scanner.ZebraWindows
{
    public class ZebraScannerProvider : IScannerProvider
    {
        private readonly ILogger<ZebraScannerProvider> _logger;
        private object? _coreScanner;
        private bool _isInitialized = false;

        public ZebraScannerProvider(ILogger<ZebraScannerProvider> logger)
        {
            _logger = logger;
        }

        private void EnsureInitialized()
        {
            if (_isInitialized && _coreScanner != null) return;

            try
            {
                // We use dynamic reflection to load the DLL at runtime, completely bypassing MSBuild COM issues.
                string dllPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Interop.CoreScanner.dll");
                if (!System.IO.File.Exists(dllPath))
                {
                    throw new ZebraScannerException($"Interop.CoreScanner.dll is missing at {dllPath}. Please make sure it is in the output folder.");
                }

                System.Reflection.Assembly asm = System.Reflection.Assembly.LoadFrom(dllPath);
                
                // Usually the namespace is CoreScanner, but sometimes it is Interop.CoreScanner
                Type? scannerType = asm.GetType("CoreScanner.CCoreScannerClass") ?? 
                                    asm.GetType("Interop.CoreScanner.CCoreScannerClass");

                if (scannerType == null)
                {
                    throw new ZebraScannerException("Failed to find CCoreScannerClass in the provided DLL.");
                }

                _coreScanner = Activator.CreateInstance(scannerType);

                if (_coreScanner == null)
                {
                    throw new ZebraScannerException("Failed to instantiate Zebra CoreScanner COM object. Is the Zebra SDK installed?");
                }

                // Host types to connect to: 
                // 1: ALL, 2: SNAPI, 3: SSI, 6: IBMHID, 7: NIXMODB, 8: HIDKB, 9: IBMTT, 11: SSI_BT
                short[] scannerTypes = { 1, 2, 3, 6, 7, 8, 9, 11 }; 
                int status;
                
                // Call Open via Reflection since _coreScanner is an object
                object[] args = new object[] { 0, scannerTypes, (short)scannerTypes.Length, 0 };
                
                // Reflection requires matching ref/out parameters precisely, but dynamic is much easier
                dynamic dynScanner = _coreScanner;
                dynScanner.Open(0, scannerTypes, (short)scannerTypes.Length, out status);

                if (status != 0)
                {
                    throw new ZebraScannerException($"Failed to open CoreScanner API. Status Code: {status}", status);
                }

                _isInitialized = true;
                _logger.LogInformation("Zebra CoreScanner API initialized successfully via Reflection.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Zebra Scanner Provider.");
                throw new ZebraScannerException("CoreScanner initialization failed.", ex);
            }
        }

        public Task<IEnumerable<ScannerInfo>> DiscoverScannersAsync()
        {
            EnsureInitialized();

            return Task.Run(() => 
            {
                var scanners = new List<ScannerInfo>();
                
                int status = 0;
                short numberOfScanners = 0;
                Array scannerList = null!;
                string outXML = string.Empty;

                dynamic dynScanner = _coreScanner!;
                dynScanner.GetScanners(out numberOfScanners, out scannerList, out outXML, out status);

                _logger.LogInformation($"[Zebra] GetScanners Status: {status}, ScannersCount: {numberOfScanners}");
                _logger.LogInformation($"[Zebra] GetScanners outXML: {outXML}");

                if (status != 0)
                {
                    _logger.LogWarning($"GetScanners returned non-zero status: {status}");
                    return (IEnumerable<ScannerInfo>)scanners;
                }

                if (!string.IsNullOrEmpty(outXML))
                {
                    try
                    {
                        var xml = XDocument.Parse(outXML);
                        var scannerElements = xml.Descendants("scanner");

                        foreach (var el in scannerElements)
                        {
                            var scannerId = el.Element("scannerID")?.Value ?? string.Empty;
                            var serialNumber = el.Element("serialnumber")?.Value ?? string.Empty;
                            var model = el.Element("modelnumber")?.Value ?? string.Empty;
                            var firmware = el.Element("firmware")?.Value ?? string.Empty;

                            scanners.Add(new ScannerInfo
                            {
                                Id = scannerId,
                                Name = model,
                                SerialNumber = serialNumber,
                                FirmwareVersion = firmware,
                                Brand = ScannerBrand.Zebra,
                                Type = ScannerType.Unknown // Can be parsed from GUID or model later
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to parse GetScanners XML output.");
                    }
                }

                return (IEnumerable<ScannerInfo>)scanners;
            });
        }

        public Task<IScanner> CreateScannerAsync(ScannerInfo info, ScannerConnectionInfo connection)
        {
            EnsureInitialized();
            
            // Return our scanner connection implementation
            IScanner scanner = new ZebraScannerConnection(info, _coreScanner, _logger);
            return Task.FromResult(scanner);
        }
    }
}
