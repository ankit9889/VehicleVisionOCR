using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using VehicleVisionOCR.Domain.Entities;
using VehicleVisionOCR.Infrastructure.Persistence;

namespace VehicleVisionOCR.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScanSessionController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ScanSessionController> _logger;

        public ScanSessionController(ApplicationDbContext dbContext, ILogger<ScanSessionController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartSession([FromBody] StartSessionDto request)
        {
            var session = new VehicleScan
            {
                VehicleId = request.VehicleId,
                ScannerDeviceId = request.ScannerId ?? Guid.Empty,
                Status = VehicleVisionOCR.Domain.Enums.ScanStatus.Initiated,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.VehicleScans.Add(session);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Started scan session {session.Id}");
            return Ok(session);
        }

        [HttpPost("{id}/stop")]
        public async Task<IActionResult> StopSession(Guid id)
        {
            var session = await _dbContext.VehicleScans.FindAsync(id);
            if (session == null) return NotFound();

            session.Status = VehicleVisionOCR.Domain.Enums.ScanStatus.Completed;
            session.LastModifiedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return Ok(session);
        }
    }

    public class StartSessionDto
    {
        public Guid VehicleId { get; set; }
        public Guid? ScannerId { get; set; }
    }
}
