using System.Collections.Generic;
using VehicleVisionOCR.Domain.Common;
using VehicleVisionOCR.Domain.Enums;
using VehicleVisionOCR.Domain.ValueObjects;

namespace VehicleVisionOCR.Domain.Entities
{
    /// <summary>
    /// Represents a vehicle processed by the system.
    /// </summary>
    public class Vehicle : AuditableEntity
    {
        public VehicleVisionOCR.Domain.ValueObjects.VIN? Vin { get; set; }
        public RegistrationNumber? RegistrationNumber { get; set; }
        public EngineNumber? EngineNumber { get; set; }
        public ChassisNumber? ChassisNumber { get; set; }
        
        public VehicleType Type { get; set; } = VehicleType.Unknown;
        
        public string? Make { get; set; }
        public string? Model { get; set; }
        public int? Year { get; set; }
        public string? Color { get; set; }
        
        public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;

        /// <summary>
        /// Collection of scans associated with this vehicle.
        /// </summary>
        public ICollection<VehicleScan> Scans { get; set; } = new List<VehicleScan>();
    }
}
