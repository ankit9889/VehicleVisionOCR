using Microsoft.Extensions.Logging;
using VehicleVisionOCR.OCR.Core.Interfaces;

namespace VehicleVisionOCR.OCR.Tesseract
{
    public class TesseractOcrProvider : IOcrProvider
    {
        private readonly ILogger<TesseractOcrProvider> _logger;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public TesseractOcrProvider(ILogger<TesseractOcrProvider> logger, Microsoft.Extensions.Configuration.IConfiguration configuration = null)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public IOcrEngine CreateEngine()
        {
            _logger.LogInformation("Creating Tesseract OCR Engine instance.");
            var engineLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<TesseractOcrEngine>.Instance;
            return new TesseractOcrEngine(engineLogger, _configuration);
        }
    }
}
