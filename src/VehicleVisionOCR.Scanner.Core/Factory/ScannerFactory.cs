using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VehicleVisionOCR.Scanner.Core.Enums;
using VehicleVisionOCR.Scanner.Core.Interfaces;
using VehicleVisionOCR.Scanner.Core.Models;

namespace VehicleVisionOCR.Scanner.Core.Factory
{
    public class ScannerFactory : IScannerFactory
    {
        private readonly IEnumerable<IScannerPlugin> _plugins;
        private readonly ILogger<ScannerFactory> _logger;

        public ScannerFactory(IEnumerable<IScannerPlugin> plugins, ILogger<ScannerFactory> logger)
        {
            _plugins = plugins;
            _logger = logger;
        }

        public async Task<IScanner> CreateAsync(ScannerBrand brand, ScannerInfo info, ScannerConnectionInfo connectionInfo)
        {
            var plugin = _plugins.FirstOrDefault(p => p.SupportedBrand == brand);
            
            if (plugin == null)
            {
                _logger.LogError($"No plugin registered for scanner brand: {brand}");
                throw new NotSupportedException($"Scanner brand {brand} is not supported by any loaded plugins.");
            }

            var provider = plugin.CreateProvider();
            var scanner = await provider.CreateScannerAsync(info, connectionInfo);
            
            _logger.LogInformation($"Created scanner instance for {brand} using plugin {plugin.PluginName}");
            return scanner;
        }
    }
}
