using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VehicleVisionOCR.Domain.Entities;
using VehicleVisionOCR.Domain.ValueObjects;

namespace VehicleVisionOCR.Application.Interfaces.Repositories
{
    public interface IVehicleRepository
    {
        Task<Vehicle?> GetByIdAsync(Guid id);
        Task<Vehicle?> GetByVinAsync(VIN vin);
        Task<Vehicle?> GetByRegistrationNumberAsync(RegistrationNumber registrationNumber);
        Task AddAsync(Vehicle vehicle);
        Task UpdateAsync(Vehicle vehicle);
        Task<IEnumerable<Vehicle>> GetPendingSyncVehiclesAsync();
    }
}
