using System;

namespace VehicleVisionOCR.Domain.Common
{
    /// <summary>
    /// Base class for entities that require auditing metadata.
    /// </summary>
    public abstract class AuditableEntity : BaseEntity
    {
        /// <summary>
        /// Gets or sets the timestamp when the entity was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the identifier of the user who created the entity.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the entity was last modified.
        /// </summary>
        public DateTime? LastModifiedAt { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who last modified the entity.
        /// </summary>
        public string? LastModifiedBy { get; set; }
    }
}
