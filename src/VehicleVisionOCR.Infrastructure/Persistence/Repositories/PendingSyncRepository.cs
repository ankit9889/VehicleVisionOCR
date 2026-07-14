using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VehicleVisionOCR.Domain.Entities;
using VehicleVisionOCR.Domain.Enums;

namespace VehicleVisionOCR.Infrastructure.Persistence.Repositories
{
    public interface IPendingSyncRepository
    {
        Task<IEnumerable<PendingSync>> GetPendingAsync(int limit);
    }

    public class PendingSyncRepository : RepositoryBase<PendingSync>, IPendingSyncRepository
    {
        public PendingSyncRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<PendingSync>> GetPendingAsync(int limit)
        {
            return await _dbContext.PendingSyncs
                .Where(p => p.Status == SyncStatus.Pending || p.Status == SyncStatus.Failed)
                .OrderBy(p => p.QueuedAt)
                .Take(limit)
                .ToListAsync();
        }
    }
}
