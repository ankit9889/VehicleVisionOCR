using Microsoft.Extensions.Logging;
using VehicleVisionOCR.OCR.Core.Interfaces;

namespace VehicleVisionOCR.OCR.Tesseract
{
    public class PositionBasedOcrProvider : IOcrProvider
    {
        private readonly ILogger _logger;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public PositionBasedOcrProvider(ILogger logger, Microsoft.Extensions.Configuration.IConfiguration configuration = null)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public string ProviderName => "PositionBased OCR Provider";

        public IOcrEngine CreateEngine()
        {
            return new PositionBasedOcrEngine(_logger, _configuration);
        }
    }
}
