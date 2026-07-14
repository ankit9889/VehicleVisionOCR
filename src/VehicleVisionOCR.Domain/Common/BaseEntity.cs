using System;

namespace VehicleVisionOCR.Domain.Common
{
    /// <summary>
    /// Base class for all domain entities providing a unique identifier.
    /// </summary>
    public abstract class BaseEntity : IEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier of the entity.
        /// </summary>
        public Guid Id { get; protected set; } = Guid.NewGuid();
    }
}
