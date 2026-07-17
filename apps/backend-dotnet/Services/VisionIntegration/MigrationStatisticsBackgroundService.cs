using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VehicleVisionOCR.Backend.Services.VisionIntegration.Models;

namespace VehicleVisionOCR.Backend.Services.VisionIntegration
{
    public class MigrationStatisticsBackgroundService : BackgroundService
    {
        private readonly ChannelReader<ComparisonResult> _reader;
        private readonly ILogger<MigrationStatisticsBackgroundService> _logger;

        public MigrationStatisticsBackgroundService(ChannelReader<ComparisonResult> reader, ILogger<MigrationStatisticsBackgroundService> logger)
        {
            _reader = reader;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var result in _reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    // Here we would create a new IServiceScope, resolve DbContext, and insert.
                    // For example:
                    // using var scope = _serviceScopeFactory.CreateScope();
                    // var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    // await dbContext.PipelineComparisonLogs.AddAsync(result, stoppingToken);
                    // await dbContext.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation("Silently logged Shadow Mode comparison for Legacy: {LegacyVin} vs Modern: {ModernVin}", result.LegacyVin, result.ModernVin);
                }
                catch (System.Exception ex)
                {
                    // Catch everything so the background worker never crashes
                    _logger.LogError(ex, "Failed to persist pipeline comparison statistics.");
                }
            }
        }
    }
}
