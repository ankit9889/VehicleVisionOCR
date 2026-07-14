using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VehicleVisionOCR.Backend.BackgroundServices
{
    public class SystemMaintenanceService : BackgroundService
    {
        private readonly ILogger<SystemMaintenanceService> _logger;
        private readonly string _imageStoragePath;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24);

        public SystemMaintenanceService(ILogger<SystemMaintenanceService> logger)
        {
            _logger = logger;
            _imageStoragePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "Images");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("System Maintenance Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    PerformImageCleanup();
                    PerformDatabaseBackup();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during system maintenance.");
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }
        }

        private void PerformImageCleanup()
        {
            if (!Directory.Exists(_imageStoragePath)) return;

            var files = Directory.GetFiles(_imageStoragePath);
            int deleted = 0;

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTimeUtc < DateTime.UtcNow.AddDays(-30))
                {
                    fileInfo.Delete();
                    deleted++;
                }
            }

            if (deleted > 0)
                _logger.LogInformation($"Cleaned up {deleted} old images.");
        }

        private void PerformDatabaseBackup()
        {
            string dbPath = "VehicleVisionOCR.db";
            string backupPath = $"VehicleVisionOCR_Backup_{DateTime.Now:yyyyMMdd}.db";

            if (File.Exists(dbPath))
            {
                File.Copy(dbPath, backupPath, overwrite: true);
                _logger.LogInformation($"Database backup created: {backupPath}");
            }
        }
    }
}
