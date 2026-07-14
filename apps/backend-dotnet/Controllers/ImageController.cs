using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace VehicleVisionOCR.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly string _storagePath;
        private readonly ILogger<ImageController> _logger;

        public ImageController(ILogger<ImageController> logger)
        {
            _logger = logger;
            _storagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "Images");
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        [HttpPost("capture")]
        public async Task<IActionResult> CaptureImage([FromBody] CaptureImageRequest request)
        {
            try
            {
                var imageBytes = Convert.FromBase64String(request.Base64Image);
                var fileName = $"capture_{DateTime.UtcNow:yyyyMMddHHmmssfff}.png";
                var filePath = Path.Combine(_storagePath, fileName);
                
                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
                
                _logger.LogInformation($"Image captured and saved: {fileName}");
                
                return Ok(new { FileName = fileName, Size = imageBytes.Length });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to capture image.");
                return BadRequest("Invalid image data");
            }
        }

        [HttpGet("{fileName}")]
        public IActionResult GetImage(string fileName)
        {
            var filePath = Path.Combine(_storagePath, fileName);
            if (!System.IO.File.Exists(filePath)) return NotFound();

            var image = System.IO.File.OpenRead(filePath);
            return File(image, "image/png");
        }
    }

    public class CaptureImageRequest
    {
        public string Base64Image { get; set; } = string.Empty;
    }
}
