using System;
using System.Threading;
using System.Threading.Tasks;
using VehicleVisionOCR.Application.Interfaces.Repositories;

namespace VehicleVisionOCR.Infrastructure.Persistence.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _dbContext;

        public IVehicleRepository Vehicles { get; }
        public IScanRepository Scans { get; }
        public IUserRepository Users { get; }
        public ISettingsRepository Settings { get; }
        public IPendingSyncRepository PendingSyncs { get; }

        public UnitOfWork(
            ApplicationDbContext dbContext,
            IVehicleRepository vehicles,
            IScanRepository scans,
            IUserRepository users,
            ISettingsRepository settings,
            IPendingSyncRepository pendingSyncs)
        {
            _dbContext = dbContext;
            Vehicles = vehicles;
            Scans = scans;
            Users = users;
            Settings = settings;
            PendingSyncs = pendingSyncs;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
