using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using VehicleVisionOCR.Infrastructure.Persistence;

namespace VehicleVisionOCR.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehiclesController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public VehiclesController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
                var scansDb = await _dbContext.VehicleScans
                .Include(s => s.Vehicle)
                .Include(s => s.Images)
                .Where(s => s.Status == Domain.Enums.ScanStatus.Completed || s.Status == Domain.Enums.ScanStatus.Failed || s.Status == Domain.Enums.ScanStatus.Initiated)
                .OrderByDescending(s => s.CreatedAt)
                .Take(50)
                .ToListAsync();

            var scans = scansDb.Select(s => new {
                    id = s.Id,
                    date = s.CreatedAt.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture),
                    vin = s.Vehicle != null && s.Vehicle.Vin != null ? s.Vehicle.Vin.Value : "N/A",
                    plate = s.Vehicle != null && s.Vehicle.RegistrationNumber != null ? s.Vehicle.RegistrationNumber.Value : "N/A",
                    status = s.Status.ToString(),
                    rawText = s.RawExtractedText,
                    make = s.Vehicle?.Make,
                    model = s.Vehicle?.Model,
                    year = s.Vehicle?.Year,
                    color = s.Vehicle?.Color,
                    imageId = s.Images.FirstOrDefault()?.Id
                })
                .ToList();

            return Ok(scans);
        }

        [HttpGet("queue")]
        public async Task<IActionResult> GetPendingQueue()
        {
            var scans = await _dbContext.VehicleScans
                .Include(s => s.Vehicle)
                .Include(s => s.Images)
                .Where(s => s.Status == Domain.Enums.ScanStatus.Initiated)
                .OrderBy(s => s.CreatedAt)
                .Select(s => new {
                    success = true,
                    scanId = s.Id,
                    vehicleId = s.Vehicle.Id,
                    vin = s.Vehicle.Vin != null ? s.Vehicle.Vin.Value : null,
                    registrationNumber = s.Vehicle.RegistrationNumber != null ? s.Vehicle.RegistrationNumber.Value : null,
                    color = s.Vehicle.Color,
                    confidence = 100, // Dummy value since we don't store confidence in DB yet
                    imageId = s.Images.FirstOrDefault() != null ? (Guid?)s.Images.FirstOrDefault().Id : null,
                    rawText = s.RawExtractedText
                })
                .ToListAsync();

            return Ok(scans);
        }

        [HttpGet("image/{imageId}")]
        public async Task<IActionResult> GetImage(Guid imageId)
        {
            var img = await _dbContext.Set<Domain.Entities.ScanImage>().FindAsync(imageId);
            if (img == null || string.IsNullOrEmpty(img.LocalFilePath) || !System.IO.File.Exists(img.LocalFilePath))
                return NotFound();

            return PhysicalFile(img.LocalFilePath, img.ContentType ?? "image/jpeg");
        }

        [HttpGet("image/file/{fileName}")]
        public IActionResult GetImageByFileName(string fileName)
        {
            var filePath = System.IO.Path.Combine(System.Environment.CurrentDirectory, "ScanData", "Images", fileName);
            if (!System.IO.File.Exists(filePath)) return NotFound();
            return PhysicalFile(filePath, "image/jpeg");
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var today = System.DateTime.UtcNow.Date;
            var todayScans = await _dbContext.VehicleScans
                .Where(s => s.CreatedAt >= today)
                .CountAsync();

            var pendingOcr = await _dbContext.VehicleScans
                .Where(s => s.Status == Domain.Enums.ScanStatus.Initiated || s.Status == Domain.Enums.ScanStatus.ProcessingOcr)
                .CountAsync();

            return Ok(new { todayScans, pendingOcr });
        }

        [HttpPost("manual")]
        public async Task<IActionResult> AddManualScan([FromBody] ManualScanRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Vin) && string.IsNullOrWhiteSpace(request.RegistrationNumber))
                return BadRequest("Either VIN or Barcode (Registration Number) is required.");

            Domain.ValueObjects.VIN? vinObj = null;
            if (!string.IsNullOrWhiteSpace(request.Vin))
            {
                var vinString = request.Vin.Trim().ToUpperInvariant();
                if (vinString.Length < 17)
                    vinString = vinString.PadRight(17, '0');
                else if (vinString.Length > 17)
                    vinString = vinString.Substring(0, 17);
                vinObj = new Domain.ValueObjects.VIN(vinString);
            }

            var regNum = !string.IsNullOrEmpty(request.RegistrationNumber) ? new Domain.ValueObjects.RegistrationNumber(request.RegistrationNumber) : null;
            
            if (regNum != null)
            {
                var existing = await _dbContext.Vehicles.FirstOrDefaultAsync(v => v.RegistrationNumber != null && v.RegistrationNumber.Value == regNum.Value);
                if (existing != null)
                {
                    return Conflict(new { 
                        error = $"Duplicate Barcode Detected: {request.RegistrationNumber}. Entry rejected.",
                        vehicleId = existing.Id
                    });
                }
            }

            var vehicle = new Domain.Entities.Vehicle
            {
                Vin = vinObj,
                Color = request.Color,
                RegistrationNumber = regNum
            };

            _dbContext.Vehicles.Add(vehicle);

            var scan = new Domain.Entities.VehicleScan
            {
                Vehicle = vehicle,
                Status = Domain.Enums.ScanStatus.Completed,
                RawExtractedText = request.RawText ?? "MANUAL ENTRY",
                ScanTime = System.DateTime.UtcNow
            };

            _dbContext.VehicleScans.Add(scan);
            await _dbContext.SaveChangesAsync();

            return Ok(new { success = true, id = scan.Id });
        }

        [HttpPost("update-duplicate")]
        public async Task<IActionResult> UpdateDuplicate([FromBody] UpdateDuplicateRequest request)
        {
            var vehicle = await _dbContext.Vehicles.FindAsync(request.VehicleId);
            if (vehicle == null) return NotFound("Vehicle not found.");

            if (!string.IsNullOrEmpty(request.Color)) 
                vehicle.Color = request.Color;

            var scan = new Domain.Entities.VehicleScan
            {
                VehicleId = vehicle.Id,
                Status = Domain.Enums.ScanStatus.Completed,
                RawExtractedText = request.RawText ?? "UPDATED ENTRY",
                ScanTime = System.DateTime.UtcNow
            };

            if (!string.IsNullOrEmpty(request.ImageFileName))
            {
                scan.Images.Add(new Domain.Entities.ScanImage
                {
                    LocalFilePath = System.IO.Path.Combine(System.Environment.CurrentDirectory, "ScanData", "Images", request.ImageFileName),
                    FileName = request.ImageFileName,
                    ContentType = "image/jpeg",
                    FileSizeBytes = 0
                });
            }

            _dbContext.VehicleScans.Add(scan);
            await _dbContext.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpDelete("clear-history")]
        public async Task<IActionResult> ClearHistory()
        {
            _dbContext.ScanImages.RemoveRange(_dbContext.ScanImages);
            _dbContext.VehicleScans.RemoveRange(_dbContext.VehicleScans);
            _dbContext.OCRResults.RemoveRange(_dbContext.OCRResults);
            _dbContext.Vehicles.RemoveRange(_dbContext.Vehicles);
            await _dbContext.SaveChangesAsync();

            // Also clear images from disk
            var imgDir = System.IO.Path.Combine(System.Environment.CurrentDirectory, "ScanData", "Images");
            if (System.IO.Directory.Exists(imgDir))
            {
                var files = System.IO.Directory.GetFiles(imgDir);
                foreach (var file in files)
                {
                    try { System.IO.File.Delete(file); } catch { }
                }
            }

            return Ok(new { success = true, message = "History cleared" });
        }

        [HttpDelete("history/{id}")]
        public async Task<IActionResult> DeleteHistoryItem(Guid id)
        {
            var scan = await _dbContext.VehicleScans.FindAsync(id);
            if (scan == null) return NotFound("Scan not found");

            var vehicleId = scan.VehicleId;

            var ocrResults = await _dbContext.OCRResults.Where(o => o.VehicleScanId == id).ToListAsync();
            var images = await _dbContext.ScanImages.Where(i => i.VehicleScanId == id).ToListAsync();

            // Delete images from disk
            var imgDir = System.IO.Path.Combine(System.Environment.CurrentDirectory, "ScanData", "Images");
            foreach (var img in images)
            {
                if (!string.IsNullOrEmpty(img.FileName))
                {
                    var filePath = System.IO.Path.Combine(imgDir, img.FileName);
                    if (System.IO.File.Exists(filePath))
                    {
                        try { System.IO.File.Delete(filePath); } catch { }
                    }
                }
            }

            _dbContext.ScanImages.RemoveRange(images);
            _dbContext.OCRResults.RemoveRange(ocrResults);
            _dbContext.VehicleScans.Remove(scan);
            
            // Check if vehicle has other scans. If not, delete it.
            var otherScans = await _dbContext.VehicleScans.AnyAsync(s => s.VehicleId == vehicleId && s.Id != id);
            if (!otherScans)
            {
                var vehicle = await _dbContext.Vehicles.FindAsync(vehicleId);
                if (vehicle != null) _dbContext.Vehicles.Remove(vehicle);
            }
            
            await _dbContext.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpPost("history/bulk-delete")]
        public async Task<IActionResult> BulkDeleteHistory([FromBody] BulkDeleteRequest request)
        {
            if (request.Ids == null || !request.Ids.Any())
                return BadRequest("No IDs provided");

            var scans = await _dbContext.VehicleScans.Where(s => request.Ids.Contains(s.Id)).ToListAsync();
            var scanIds = scans.Select(s => s.Id).ToList();
            var vehicleIds = scans.Select(s => s.VehicleId).Distinct().ToList();

            var ocrResults = await _dbContext.OCRResults.Where(o => scanIds.Contains(o.VehicleScanId)).ToListAsync();
            var images = await _dbContext.ScanImages.Where(i => scanIds.Contains(i.VehicleScanId)).ToListAsync();

            var imgDir = System.IO.Path.Combine(System.Environment.CurrentDirectory, "ScanData", "Images");
            foreach (var img in images)
            {
                if (!string.IsNullOrEmpty(img.FileName))
                {
                    var filePath = System.IO.Path.Combine(imgDir, img.FileName);
                    if (System.IO.File.Exists(filePath))
                    {
                        try { System.IO.File.Delete(filePath); } catch { }
                    }
                }
            }

            _dbContext.ScanImages.RemoveRange(images);
            _dbContext.OCRResults.RemoveRange(ocrResults);
            _dbContext.VehicleScans.RemoveRange(scans);
            
            // Cleanup orphaned vehicles
            foreach (var vId in vehicleIds)
            {
                var hasOtherScans = await _dbContext.VehicleScans.AnyAsync(s => s.VehicleId == vId && !scanIds.Contains(s.Id));
                if (!hasOtherScans)
                {
                    var vehicle = await _dbContext.Vehicles.FindAsync(vId);
                    if (vehicle != null) _dbContext.Vehicles.Remove(vehicle);
                }
            }
            
            await _dbContext.SaveChangesAsync();

            return Ok(new { success = true, deletedCount = scans.Count });
        }

        [HttpPost("confirm-scan")]
        public async Task<IActionResult> ConfirmScan([FromBody] ConfirmScanRequest request)
        {
            var scan = await _dbContext.VehicleScans.FirstOrDefaultAsync(s => s.VehicleId == request.VehicleId);
            if (scan == null) return NotFound("Scan not found.");

            scan.Status = Domain.Enums.ScanStatus.Completed;
            await _dbContext.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }

    public class ConfirmScanRequest
    {
        public Guid VehicleId { get; set; }
    }

    public class BulkDeleteRequest
    {
        public List<Guid> Ids { get; set; } = new List<Guid>();
    }

    public class ManualScanRequest
    {
        public string? Vin { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? Color { get; set; }
        public string? RawText { get; set; }
    }

    public class UpdateDuplicateRequest
    {
        public Guid VehicleId { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? Color { get; set; }
        public string? RawText { get; set; }
        public string? ImageFileName { get; set; }
    }
}
