using System;
using System.Collections.Generic;
using VehicleVisionOCR.Domain.Common;
using VehicleVisionOCR.Domain.Enums;

namespace VehicleVisionOCR.Domain.Entities
{
    /// <summary>
    /// Represents a single scanning event for a vehicle.
    /// </summary>
    public class VehicleScan : AuditableEntity
    {
        public Guid VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }
        
        public Guid? ScannerDeviceId { get; set; }
        public ScannerDevice? ScannerDevice { get; set; }

        public ScanStatus Status { get; set; } = ScanStatus.Initiated;
        public DateTime ScanTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The raw extracted text before structuring.
        /// </summary>
        public string? RawExtractedText { get; set; }

        public ICollection<ScanImage> Images { get; set; } = new List<ScanImage>();
        public ICollection<OCRResult> OCRResults { get; set; } = new List<OCRResult>();
    }
}
