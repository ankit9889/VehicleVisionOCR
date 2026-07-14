using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VehicleVisionOCR.Scanner.Core.Interfaces;
using VehicleVisionOCR.Scanner.MobileWeb;

namespace VehicleVisionOCR.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MobileScannerController : ControllerBase
    {
        private readonly IScannerManager _scannerManager;
        private readonly ILogger<MobileScannerController> _logger;

        public MobileScannerController(IScannerManager scannerManager, ILogger<MobileScannerController> logger)
        {
            _scannerManager = scannerManager;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest("No image provided.");

            try
            {
                // Find the active mobile scanner
                var activeMobileScanner = _scannerManager.ActiveScanners
                    .FirstOrDefault(s => s is MobileScanner) as MobileScanner;

                if (activeMobileScanner == null)
                {
                    // Try to find it in available scanners and auto-connect
                    var availableScanners = await _scannerManager.DiscoverScannersAsync();
                    var availableMobileScannerInfo = availableScanners
                        .FirstOrDefault(s => s.Id == "MOB-WEB-001");
                        
                    if (availableMobileScannerInfo != null)
                    {
                        var brand = availableMobileScannerInfo.Brand;
                        var connInfo = new Scanner.Core.Models.ScannerConnectionInfo 
                        { 
                            PortOrAddress = availableMobileScannerInfo.Id, 
                            ConnectionType = Scanner.Core.Enums.ConnectionType.USB 
                        };
                        var connectedScanner = await _scannerManager.ConnectScannerAsync(brand, connInfo);
                        
                        // Apply the requested ID since ScannerManager generates a random one
                        var prop = typeof(Scanner.Core.Models.ScannerInfo).GetProperty("Id");
                        if (prop != null && prop.CanWrite)
                        {
                            prop.SetValue(connectedScanner.Info, availableMobileScannerInfo.Id);
                        }

                        activeMobileScanner = connectedScanner as MobileScanner;
                    }
                }

                if (activeMobileScanner == null)
                {
                    return BadRequest("Mobile Web Scanner could not be found or connected.");
                }

                using var memoryStream = new MemoryStream();
                await image.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();

                _logger.LogInformation($"Received mobile scan image of size {imageBytes.Length} bytes.");

                // Pass the image to the scanner instance which raises the event
                activeMobileScanner.ReceiveImage(imageBytes);

                return Ok(new { Message = "Image processed successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing uploaded mobile scan.");
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}
