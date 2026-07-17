using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using VehicleVisionOCR.OCR.Core.Interfaces;

namespace VehicleVisionOCR.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public partial class OcrController : ControllerBase
    {
        private readonly IOcrManager _ocrManager;
        private readonly ILogger<OcrController> _logger;
        private readonly VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces.IOcrCorrectionCoordinator _correctionCoordinator;

        [System.Text.RegularExpressions.GeneratedRegex(@"\s+")]
        private static partial System.Text.RegularExpressions.Regex WhitespaceRegex();

        public OcrController(IOcrManager ocrManager, ILogger<OcrController> logger, VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces.IOcrCorrectionCoordinator correctionCoordinator)
        {
            _ocrManager = ocrManager;
            _logger = logger;
            _correctionCoordinator = correctionCoordinator;
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
                var engineType = request.UsePositionBasedExtraction ? VehicleVisionOCR.OCR.Core.Enums.OcrEngineType.PositionBased : VehicleVisionOCR.OCR.Core.Enums.OcrEngineType.Tesseract;
                var result = await _ocrManager.ProcessAsync(ocrRequest, engineType);
                
                // Apply correction to the extracted fields
                if (result.Status == VehicleVisionOCR.OCR.Core.Enums.OcrStatus.Success && result.Result != null)
                {
                    var vinField = result.Result.ExtractedFields.Find(f => f.Key == "VIN");
                    if (vinField != null)
                    {
                        var vinResult = await _correctionCoordinator.ProcessFieldAsync(
                            VehicleVisionOCR.Backend.Services.OcrCorrection.Enums.TargetFieldType.VIN, 
                            vinField.Value, 
                            result.Result.OverallConfidence.Percentage);
                            
                        if (vinResult.IsValid)
                        {
                            vinField.Value = vinResult.CorrectedText;
                        }
                    }

                    var colorField = result.Result.ExtractedFields.Find(f => f.Key.Equals("Color", StringComparison.OrdinalIgnoreCase));
                    if (colorField != null)
                    {
                        var colorResult = await _correctionCoordinator.ProcessFieldAsync(
                            VehicleVisionOCR.Backend.Services.OcrCorrection.Enums.TargetFieldType.Color, 
                            colorField.Value, 
                            result.Result.OverallConfidence.Percentage);
                            
                        if (colorResult.IsValid)
                        {
                            colorField.Value = colorResult.CorrectedText;
                        }
                    }
                }

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
        public bool UsePositionBasedExtraction { get; set; }
    }
}
