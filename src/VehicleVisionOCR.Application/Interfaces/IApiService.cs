using System.Threading.Tasks;
using VehicleVisionOCR.Application.Common.Models;
using VehicleVisionOCR.Domain.Entities;

namespace VehicleVisionOCR.Application.Interfaces
{
    /// <summary>
    /// High-level API service for synchronizing data with Laravel backend.
    /// </summary>
    public interface IApiService
    {
        Task<Result<bool>> SyncVehicleScanAsync(VehicleScan scan);
        Task<Result<bool>> UploadImageAsync(ScanImage image);
        Task<Result<bool>> CheckApiHealthAsync();
    }
}
