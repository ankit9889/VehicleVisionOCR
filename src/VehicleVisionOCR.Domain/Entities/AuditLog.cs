using System;
using VehicleVisionOCR.Domain.Common;
using VehicleVisionOCR.Domain.Enums;

namespace VehicleVisionOCR.Domain.Entities
{
    /// <summary>
    /// Represents a record of a significant system or user action.
    /// </summary>
    public class AuditLog : BaseEntity
    {
        public LogType Type { get; set; } = LogType.Info;
        public string Message { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string? Action { get; set; }
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
