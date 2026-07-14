using System.Threading.Tasks;

namespace VehicleVisionOCR.Application.Interfaces
{
    /// <summary>
    /// Manages reading, saving, and deleting local scan images.
    /// </summary>
    public interface IImageStorageService
    {
        Task<string> SaveImageAsync(byte[] imageBytes, string fileName);
        Task DeleteImageAsync(string filePath);
        Task<byte[]> ReadImageAsync(string filePath);
    }
}
