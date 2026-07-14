using System;
using VehicleVisionOCR.Domain.Common;

namespace VehicleVisionOCR.Domain.Entities
{
    /// <summary>
    /// Represents an image captured during a scan operation.
    /// </summary>
    public class ScanImage : AuditableEntity
    {
        public Guid VehicleScanId { get; set; }
        public VehicleScan? VehicleScan { get; set; }

        /// <summary>
        /// Local file system path to the image.
        /// </summary>
        public string LocalFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Remote URL after the image is synchronized to the API.
        /// </summary>
        public string? RemoteUrl { get; set; }

        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "image/jpeg";
        public long FileSizeBytes { get; set; }
        
        /// <summary>
        /// True if the image has been successfully uploaded to the backend API.
        /// </summary>
        public bool IsSynced { get; set; } = false;
    }
}
