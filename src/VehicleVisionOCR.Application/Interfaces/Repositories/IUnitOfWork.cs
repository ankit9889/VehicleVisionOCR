using System;
using System.Threading;
using System.Threading.Tasks;

namespace VehicleVisionOCR.Application.Interfaces.Repositories
{
    /// <summary>
    /// Enforces atomicity for database operations.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        IVehicleRepository Vehicles { get; }
        IScanRepository Scans { get; }
        
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
