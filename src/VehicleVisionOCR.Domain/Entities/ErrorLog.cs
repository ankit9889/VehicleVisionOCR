using System;
using VehicleVisionOCR.Domain.Common;

namespace VehicleVisionOCR.Domain.Entities
{
    /// <summary>
    /// Represents an application exception or failure state.
    /// </summary>
    public class ErrorLog : BaseEntity
    {
        public string ErrorMessage { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public string? Source { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public bool IsResolved { get; set; } = false;
    }
}
