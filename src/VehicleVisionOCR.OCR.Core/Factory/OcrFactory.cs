using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using VehicleVisionOCR.OCR.Core.Enums;
using VehicleVisionOCR.OCR.Core.Interfaces;

namespace VehicleVisionOCR.OCR.Core.Factory
{
    public class OcrFactory : IOcrFactory
    {
        private readonly IEnumerable<IOcrPlugin> _plugins;
        private readonly ILogger<OcrFactory> _logger;

        public OcrFactory(IEnumerable<IOcrPlugin> plugins, ILogger<OcrFactory> logger)
        {
            _plugins = plugins;
            _logger = logger;
        }

        public IOcrEngine CreateEngine(OcrEngineType engineType)
        {
            var plugin = _plugins.FirstOrDefault(p => p.SupportedEngine == engineType);

            if (plugin == null)
            {
                _logger.LogError($"No OCR plugin registered for engine type: {engineType}");
                throw new NotSupportedException($"OCR engine {engineType} is not supported by any loaded plugins.");
            }

            var provider = plugin.CreateProvider();
            var engine = provider.CreateEngine();

            _logger.LogInformation($"Created OCR engine instance for {engineType} using plugin {plugin.PluginName}");
            return engine;
        }
    }
}
