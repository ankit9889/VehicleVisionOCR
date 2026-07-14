using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace VehicleVisionOCR.Infrastructure.BackgroundServices
{
    public class SyncBackgroundService : BackgroundService
    {
        private readonly ILogger<SyncBackgroundService> _logger;
        // private readonly IServiceProvider _serviceProvider; // For creating scope to resolve scoped services

        public SyncBackgroundService(ILogger<SyncBackgroundService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SyncBackgroundService is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Logic to process PendingSync repository offline queue
                    // Resolve ISyncService from scope here when implementing business logic
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing SyncBackgroundService.");
                }
            }

            _logger.LogInformation("SyncBackgroundService is stopping.");
        }
    }
}
