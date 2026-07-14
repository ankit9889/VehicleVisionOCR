using System;
using VehicleVisionOCR.Domain.Common;
using VehicleVisionOCR.Domain.Enums;
using VehicleVisionOCR.Domain.ValueObjects;

namespace VehicleVisionOCR.Domain.Entities
{
    /// <summary>
    /// Represents the structured data extracted via OCR for a specific scan.
    /// </summary>
    public class OCRResult : AuditableEntity
    {
        public Guid VehicleScanId { get; set; }
        public VehicleScan? VehicleScan { get; set; }

        public string FieldName { get; set; } = string.Empty;
        public string ExtractedValue { get; set; } = string.Empty;
        
        public ConfidenceScore Confidence { get; set; } = new ConfidenceScore(0);
        public OCRStatus Status { get; set; } = OCRStatus.Pending;

        /// <summary>
        /// Time taken by the OCR engine to process the extraction (in milliseconds).
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// True if the operator manually overrode the OCR value.
        /// </summary>
        public bool IsManuallyCorrected { get; set; } = false;
    }
}
