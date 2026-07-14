using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleVisionOCR.Infrastructure.Persistence;
using VehicleVisionOCR.Domain.Entities;
using System.Threading.Tasks;

namespace VehicleVisionOCR.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ColorsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public ColorsController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetColors()
        {
            var colors = await _dbContext.VehicleColors
                .OrderBy(c => c.Name)
                .Select(c => c.Name)
                .ToListAsync();
            return Ok(colors);
        }

        [HttpPost]
        public async Task<IActionResult> AddColor([FromBody] AddColorRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Color name cannot be empty.");

            var upperName = request.Name.Trim().ToUpperInvariant();

            if (await _dbContext.VehicleColors.AnyAsync(c => c.Name == upperName))
                return Conflict("Color already exists.");

            var newColor = new VehicleColor
            {
                Name = upperName
            };

            _dbContext.VehicleColors.Add(newColor);
            await _dbContext.SaveChangesAsync();

            return Ok(new { success = true, color = newColor.Name });
        }

        [HttpDelete("{name}")]
        public async Task<IActionResult> DeleteColor(string name)
        {
            var upperName = name.Trim().ToUpperInvariant();
            var color = await _dbContext.VehicleColors.FirstOrDefaultAsync(c => c.Name == upperName);
            
            if (color == null)
                return NotFound("Color not found.");

            _dbContext.VehicleColors.Remove(color);
            await _dbContext.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }

    public class AddColorRequest
    {
        public string? Name { get; set; }
    }
}
