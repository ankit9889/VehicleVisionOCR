using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VehicleVisionOCR.Application.Interfaces.Repositories;
using VehicleVisionOCR.Domain.Entities;
using VehicleVisionOCR.Domain.ValueObjects;

namespace VehicleVisionOCR.Infrastructure.Persistence.Repositories
{
    public class VehicleRepository : RepositoryBase<Vehicle>, IVehicleRepository
    {
        public VehicleRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<Vehicle?> GetByVinAsync(VIN vin)
        {
            return await _dbContext.Vehicles.FirstOrDefaultAsync(x => x.Vin != null && x.Vin.Value == vin.Value);
        }

        public async Task<Vehicle?> GetByRegistrationNumberAsync(RegistrationNumber registrationNumber)
        {
            return await _dbContext.Vehicles.FirstOrDefaultAsync(x => x.RegistrationNumber != null && x.RegistrationNumber.Value == registrationNumber.Value);
        }

        public async Task<IEnumerable<Vehicle>> GetPendingSyncVehiclesAsync()
        {
            // Placeholder logic to return vehicles that might be in the pending sync table
            var pendingVins = await _dbContext.PendingSyncs
                .Where(p => p.Endpoint.Contains("/api/vehicles"))
                .Select(p => p.Id) 
                .ToListAsync();

            return await _dbContext.Vehicles
                .Where(v => pendingVins.Contains(v.Id))
                .ToListAsync();
        }
    }
}
