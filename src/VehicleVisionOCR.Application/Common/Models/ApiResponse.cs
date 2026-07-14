using System.Collections.Generic;

namespace VehicleVisionOCR.Application.Common.Models
{
    /// <summary>
    /// Standardized API response format for external communications.
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Payload { get; set; }
        public string? Message { get; set; }
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
    }
}
