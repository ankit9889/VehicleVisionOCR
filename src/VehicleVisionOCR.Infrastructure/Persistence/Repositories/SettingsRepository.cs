using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using VehicleVisionOCR.Domain.Entities;

namespace VehicleVisionOCR.Infrastructure.Persistence.Repositories
{
    public interface ISettingsRepository
    {
        Task<ApplicationSetting?> GetByKeyAsync(string key);
    }

    public class SettingsRepository : RepositoryBase<ApplicationSetting>, ISettingsRepository
    {
        public SettingsRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<ApplicationSetting?> GetByKeyAsync(string key)
        {
            return await _dbContext.ApplicationSettings.FirstOrDefaultAsync(s => s.Key == key);
        }
    }
}
