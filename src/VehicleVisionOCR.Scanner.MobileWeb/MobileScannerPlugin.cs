using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VehicleVisionOCR.Scanner.Core.Enums;
using VehicleVisionOCR.Scanner.Core.Interfaces;
using VehicleVisionOCR.Scanner.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace VehicleVisionOCR.Scanner.MobileWeb
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddMobileWebScannerPlugin(this IServiceCollection services)
        {
            services.AddSingleton<IScannerPlugin, MobileScannerPlugin>();
            return services;
        }
    }

    public class MobileScannerPlugin : IScannerPlugin
    {
        public string PluginId => "MobileWeb_Scanner";
        public string PluginName => "Mobile Web Camera Scanner";
        public string Version => "1.0.0";
        public ScannerBrand SupportedBrand => ScannerBrand.Generic;

        public IScannerProvider CreateProvider()
        {
            return new MobileScannerProvider();
        }
    }

    public class MobileScannerProvider : IScannerProvider
    {
        public Task<IEnumerable<ScannerInfo>> DiscoverScannersAsync()
        {
            var scanners = new List<ScannerInfo>
            {
                new ScannerInfo
                {
                    Id = "MOB-WEB-001",
                    Name = "Mobile Web Camera Scanner",
                    Brand = ScannerBrand.Generic,
                    SerialNumber = "S/N: NETWORK-CAM-1",
                    FirmwareVersion = "V1.0.0"
                }
            };
            return Task.FromResult<IEnumerable<ScannerInfo>>(scanners);
        }

        public Task<IScanner> CreateScannerAsync(ScannerInfo info, ScannerConnectionInfo connection)
        {
            return Task.FromResult<IScanner>(new MobileScanner(info));
        }
    }
}
