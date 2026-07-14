using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VehicleVisionOCR.Application.Interfaces.Repositories;
using VehicleVisionOCR.Domain.Entities;

namespace VehicleVisionOCR.Infrastructure.Persistence.Repositories
{
    public class ScanRepository : RepositoryBase<VehicleScan>, IScanRepository
    {
        public ScanRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<VehicleScan>> GetByVehicleIdAsync(Guid vehicleId)
        {
            return await _dbContext.VehicleScans
                .Include(s => s.Images)
                .Include(s => s.OCRResults)
                .Where(s => s.VehicleId == vehicleId)
                .ToListAsync();
        }

        public override async Task<VehicleScan?> GetByIdAsync(Guid id)
        {
            return await _dbContext.VehicleScans
                .Include(s => s.Images)
                .Include(s => s.OCRResults)
                .FirstOrDefaultAsync(s => s.Id == id);
        }
    }
}
