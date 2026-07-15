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
                var engineType = request.UsePositionBasedExtraction ? VehicleVisionOCR.OCR.Core.Enums.OcrEngineType.PositionBased : VehicleVisionOCR.OCR.Core.Enums.OcrEngineType.Tesseract;
                var result = await _ocrManager.ProcessAsync(ocrRequest, engineType);
                
                // Attempt dynamic color matching from Database for UI testing
                if (result.Status == VehicleVisionOCR.OCR.Core.Enums.OcrStatus.Success && result.Result != null && !string.IsNullOrEmpty(result.Result.RawText))
                {
                    using var scope = HttpContext.RequestServices.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<VehicleVisionOCR.Infrastructure.Persistence.ApplicationDbContext>();
                    var dbColors = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(System.Linq.Queryable.Select(dbContext.VehicleColors, c => c.Name));
                    
                    var rawTextUpper = result.Result.RawText.ToUpperInvariant().Replace("\n", " ").Replace("\r", " ");
                    rawTextUpper = System.Text.RegularExpressions.Regex.Replace(rawTextUpper, @"\s+", " ");
                    foreach (var dbColor in System.Linq.Enumerable.OrderByDescending(dbColors.Where(c => !string.IsNullOrEmpty(c)), c => c.Length))
                    {
                        if (VehicleVisionOCR.Backend.Helpers.FuzzyMatcher.IsFuzzyMatch(rawTextUpper, dbColor.ToUpperInvariant()))
                        {
                            var colorField = result.Result.ExtractedFields.Find(f => f.Key.Equals("Color", StringComparison.OrdinalIgnoreCase));
                            if (colorField != null)
                            {
                                colorField.Value = dbColor;
                            }
                            else
                            {
                                result.Result.ExtractedFields.Add(new VehicleVisionOCR.OCR.Core.Models.OcrField { Key = "Color", Value = dbColor });
                            }
                            break;
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
