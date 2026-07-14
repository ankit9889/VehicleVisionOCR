using System;

namespace VehicleVisionOCR.Domain.Common
{
    /// <summary>
    /// Defines the base contract for all entities in the domain.
    /// </summary>
    public interface IEntity
    {
        /// <summary>
        /// Gets the unique identifier of the entity.
        /// </summary>
        Guid Id { get; }
    }
}
