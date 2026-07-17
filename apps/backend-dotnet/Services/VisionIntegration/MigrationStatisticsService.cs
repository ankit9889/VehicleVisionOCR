using System.Threading.Channels;
using System.Threading.Tasks;
using VehicleVisionOCR.Backend.Services.VisionIntegration.Models;

namespace VehicleVisionOCR.Backend.Services.VisionIntegration
{
    public class MigrationStatisticsService
    {
        private readonly ChannelWriter<ComparisonResult> _writer;

        public MigrationStatisticsService(ChannelWriter<ComparisonResult> writer)
        {
            _writer = writer;
        }

        public async Task LogComparisonAsync(ComparisonResult result)
        {
            // Thread-safe, non-blocking insert into the memory queue
            await _writer.WriteAsync(result);
        }
    }
}
