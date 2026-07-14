using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using VehicleVisionOCR.Scanner.Core.Interfaces;
using VehicleVisionOCR.Scanner.Core.Models;

namespace VehicleVisionOCR.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] // Enforce local authentication later
    public class ScannerController : ControllerBase
    {
        private readonly IScannerManager _scannerManager;
        private readonly ILogger<ScannerController> _logger;

        public ScannerController(IScannerManager scannerManager, ILogger<ScannerController> logger)
        {
            _scannerManager = scannerManager;
            _logger = logger;
        }

        [HttpGet("active")]
        public IActionResult GetActiveScanners()
        {
            return Ok(_scannerManager.ActiveScanners.Select(s => new { Info = s.Info, State = s.State }));
        }

        [HttpGet("discover")]
        public async Task<IActionResult> DiscoverScanners()
        {
            try
            {
                var scanners = await _scannerManager.DiscoverScannersAsync();
                return Ok(scanners.Select(s => new { Info = s, State = "Discovered" }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error discovering scanners.");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("connect/{scannerId}")]
        public async Task<IActionResult> ConnectScanner(string scannerId)
        {
            try
            {
                var discoveredScanners = await _scannerManager.DiscoverScannersAsync();
                var scannerInfo = discoveredScanners.FirstOrDefault(s => s.Id == scannerId);
                var brand = scannerInfo?.Brand ?? VehicleVisionOCR.Scanner.Core.Enums.ScannerBrand.Zebra;
                
                var connInfo = new VehicleVisionOCR.Scanner.Core.Models.ScannerConnectionInfo 
                { 
                    PortOrAddress = scannerId, 
                    ConnectionType = VehicleVisionOCR.Scanner.Core.Enums.ConnectionType.USB 
                };
                
                // Temporary solution to pass the real ID through ScannerConnectionInfo
                // since ScannerManager ConnectScannerAsync creates a new ID.
                connInfo.PortOrAddress = scannerId; 
                
                var connectedScanner = await _scannerManager.ConnectScannerAsync(brand, connInfo);
                
                // HACK: Override the auto-generated ID with the requested ID
                // In a real refactor, ConnectScannerAsync should take ScannerInfo as a parameter.
                var prop = typeof(ScannerInfo).GetProperty("Id");
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(connectedScanner.Info, scannerId);
                }
                
                return Ok(new { Message = $"Connected to scanner {scannerId}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error connecting to scanner {scannerId}.");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("disconnect/{scannerId}")]
        public async Task<IActionResult> DisconnectScanner(string scannerId)
        {
            try
            {
                await _scannerManager.DisconnectScannerAsync(scannerId);
                return Ok(new { Message = $"Disconnected from scanner {scannerId}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error disconnecting from scanner {scannerId}.");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("status/{scannerId}")]
        public IActionResult GetStatus(string scannerId)
        {
            var scanner = System.Linq.Enumerable.FirstOrDefault(_scannerManager.ActiveScanners, s => s.Info.Id == scannerId);
            return Ok(new { ScannerId = scannerId, Status = scanner?.State.ToString() ?? "NotFound" });
        }

        [HttpPost("trigger/{scannerId}")]
        public async Task<IActionResult> TriggerScan(string scannerId)
        {
            try
            {
                var scanner = System.Linq.Enumerable.FirstOrDefault(_scannerManager.ActiveScanners, s => s.Info.Id == scannerId);
                if (scanner != null)
                {
                    await scanner.CaptureImageAsync();
                    return Ok(new { Message = "Scan triggered." });
                }
                return NotFound("Scanner not found.");
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
