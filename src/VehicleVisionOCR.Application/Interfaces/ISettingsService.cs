using System.Collections.Generic;
using System.Threading.Tasks;
using VehicleVisionOCR.Domain.Entities;

namespace VehicleVisionOCR.Application.Interfaces
{
    /// <summary>
    /// Manages local application configuration in the SQLite database.
    /// </summary>
    public interface ISettingsService
    {
        Task<string?> GetSettingAsync(string key);
        Task SetSettingAsync(string key, string value, bool encrypt = false);
        Task<IEnumerable<ApplicationSetting>> GetAllSettingsAsync();
    }
}
