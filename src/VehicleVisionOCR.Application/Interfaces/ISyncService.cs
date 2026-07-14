using System.Threading.Tasks;

namespace VehicleVisionOCR.Application.Interfaces
{
    /// <summary>
    /// Background service coordinator for processing the offline PendingSync queue.
    /// </summary>
    public interface ISyncService
    {
        Task ProcessOfflineQueueAsync();
        Task EnqueueFailedRequestAsync(string endpoint, string payload, string httpMethod, string error);
    }
}
