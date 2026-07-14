using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VehicleVisionOCR.Scanner.Core.Enums;
using VehicleVisionOCR.Scanner.Core.Interfaces;
using VehicleVisionOCR.Scanner.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace VehicleVisionOCR.Scanner.Emulator
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddEmulatorScannerPlugin(this IServiceCollection services)
        {
            services.AddSingleton<IScannerPlugin, EmulatorScannerPlugin>();
            return services;
        }
    }

    public class EmulatorScannerPlugin : IScannerPlugin
    {
        public string PluginId => "Emulator_CoreScanner";
        public string PluginName => "Emulator Scanner SDK for Windows";
        public string Version => "1.0.0";
        public ScannerBrand SupportedBrand => ScannerBrand.Emulator;

        public IScannerProvider CreateProvider()
        {
            return new EmulatorScannerProvider();
        }
    }

    public class EmulatorScannerProvider : IScannerProvider
    {
        public Task<IEnumerable<ScannerInfo>> DiscoverScannersAsync()
        {
            var scanners = new List<ScannerInfo>
            {
                new ScannerInfo
                {
                    Id = "EMU-001",
                    Name = "Zebra DS4608 Emulator",
                    Brand = ScannerBrand.Emulator,
                    SerialNumber = "S/N: 123456789EMU",
                    FirmwareVersion = "V1.0.0"
                }
            };
            return Task.FromResult<IEnumerable<ScannerInfo>>(scanners);
        }

        public Task<IScanner> CreateScannerAsync(ScannerInfo info, ScannerConnectionInfo connection)
        {
            return Task.FromResult<IScanner>(new EmulatorScanner(info));
        }
    }
}
