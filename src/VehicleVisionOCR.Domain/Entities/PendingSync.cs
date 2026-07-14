using System;
using VehicleVisionOCR.Domain.Common;
using VehicleVisionOCR.Domain.Enums;

namespace VehicleVisionOCR.Domain.Entities
{
    /// <summary>
    /// Represents an API request that failed and is queued for offline synchronization.
    /// </summary>
    public class PendingSync : BaseEntity
    {
        /// <summary>
        /// The serialized JSON payload of the request.
        /// </summary>
        public string Payload { get; set; } = string.Empty;

        /// <summary>
        /// The target API endpoint route.
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// HTTP Method (e.g., POST, PUT).
        /// </summary>
        public string HttpMethod { get; set; } = "POST";

        public int RetryCount { get; set; } = 0;
        public SyncStatus Status { get; set; } = SyncStatus.Pending;
        
        public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastAttemptAt { get; set; }
        public string? LastErrorMessage { get; set; }
    }
}
