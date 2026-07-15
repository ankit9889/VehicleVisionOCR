using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VehicleVisionOCR.OCR.Core.Enums;
using VehicleVisionOCR.OCR.Core.Interfaces;

namespace VehicleVisionOCR.OCR.Tesseract
{
    public class PositionBasedOcrPlugin : IOcrPlugin
    {
        private readonly ILogger _logger;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public PositionBasedOcrPlugin(ILogger<PositionBasedOcrProvider> logger, Microsoft.Extensions.Configuration.IConfiguration configuration = null)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public PositionBasedOcrPlugin()
        {
            _logger = NullLogger<PositionBasedOcrProvider>.Instance;
            _configuration = null;
        }

        public string PluginId => "PositionBasedOCR.V1";
        public string PluginName => "Position Based OCR Wrapper";
        public string Version => "1.0.0";
        public OcrEngineType SupportedEngine => OcrEngineType.PositionBased;

        public IOcrProvider CreateProvider()
        {
            return new PositionBasedOcrProvider(_logger, _configuration);
        }
    }
}
