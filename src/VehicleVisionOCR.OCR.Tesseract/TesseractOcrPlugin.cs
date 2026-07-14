using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VehicleVisionOCR.OCR.Core.Enums;
using VehicleVisionOCR.OCR.Core.Interfaces;

namespace VehicleVisionOCR.OCR.Tesseract
{
    public class TesseractOcrPlugin : IOcrPlugin
    {
        private readonly ILogger<TesseractOcrProvider> _providerLogger;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public TesseractOcrPlugin(ILogger<TesseractOcrProvider> providerLogger, Microsoft.Extensions.Configuration.IConfiguration configuration = null)
        {
            _providerLogger = providerLogger;
            _configuration = configuration;
        }

        // Default constructor for dynamic loading
        public TesseractOcrPlugin()
        {
            _providerLogger = NullLogger<TesseractOcrProvider>.Instance;
            _configuration = null;
        }

        public string PluginId => "TesseractOCR.V5";
        public string PluginName => "Tesseract OCR Plugin using OpenCV";
        public string Version => "5.0.0";
        public OcrEngineType SupportedEngine => OcrEngineType.Tesseract;

        public IOcrProvider CreateProvider()
        {
            return new TesseractOcrProvider(_providerLogger, _configuration);
        }
    }
}
