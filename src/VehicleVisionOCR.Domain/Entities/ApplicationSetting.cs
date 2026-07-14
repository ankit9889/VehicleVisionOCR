using VehicleVisionOCR.Domain.Common;

namespace VehicleVisionOCR.Domain.Entities
{
    /// <summary>
    /// Represents a persisted configuration setting for the local application.
    /// </summary>
    public class ApplicationSetting : AuditableEntity
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsEncrypted { get; set; } = false;
    }
}
