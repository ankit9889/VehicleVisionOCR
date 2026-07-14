using System;
using System.IO;
using System.Threading.Tasks;
using VehicleVisionOCR.Application.Interfaces;

namespace VehicleVisionOCR.Infrastructure.Storage
{
    public class LocalImageStorageService : IImageStorageService
    {
        private readonly string _baseStoragePath;

        public LocalImageStorageService()
        {
            _baseStoragePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VehicleVisionOCR", "Images");
            if (!Directory.Exists(_baseStoragePath))
            {
                Directory.CreateDirectory(_baseStoragePath);
            }
        }

        public async Task<string> SaveImageAsync(byte[] imageBytes, string fileName)
        {
            var fullPath = Path.Combine(_baseStoragePath, fileName);
            await File.WriteAllBytesAsync(fullPath, imageBytes);
            return fullPath;
        }

        public Task DeleteImageAsync(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return Task.CompletedTask;
        }

        public async Task<byte[]> ReadImageAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Image not found", filePath);
            
            return await File.ReadAllBytesAsync(filePath);
        }
    }
}
