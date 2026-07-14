using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VehicleVisionOCR.Domain.Entities;

namespace VehicleVisionOCR.Application.Interfaces.Repositories
{
    public interface IScanRepository
    {
        Task<VehicleScan?> GetByIdAsync(Guid id);
        Task<IEnumerable<VehicleScan>> GetByVehicleIdAsync(Guid vehicleId);
        Task AddAsync(VehicleScan scan);
        Task UpdateAsync(VehicleScan scan);
    }
}
