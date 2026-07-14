using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using VehicleVisionOCR.OCR.Core.Interfaces;

namespace VehicleVisionOCR.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OcrController : ControllerBase
    {
        private readonly IOcrManager _ocrManager;
        private readonly ILogger<OcrController> _logger;

        public OcrController(IOcrManager ocrManager, ILogger<OcrController> logger)
        {
            _ocrManager = ocrManager;
            _logger = logger;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessImage([FromBody] ProcessImageRequest request)
        {
            if (string.IsNullOrEmpty(request.Base64Image))
                return BadRequest("No image data provided.");

            try
            {
                byte[] imageBytes = Convert.FromBase64String(request.Base64Image);
                
                var ocrRequest = new VehicleVisionOCR.OCR.Core.Models.OcrRequest { ImageData = imageBytes };
                var result = await _ocrManager.ProcessAsync(ocrRequest, VehicleVisionOCR.OCR.Core.Enums.OcrEngineType.Tesseract);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR processing failed.");
                return StatusCode(500, ex.Message);
            }
        }
    }

    public class ProcessImageRequest
    {
        public string Base64Image { get; set; } = string.Empty;
    }
}
