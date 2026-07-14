using System;

namespace VehicleVisionOCR.Domain.ValueObjects
{
    /// <summary>
    /// Value object representing a vehicle's chassis number.
    /// </summary>
    public record ChassisNumber
    {
        public string Value { get; init; }

        public ChassisNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Chassis number cannot be empty.", nameof(value));

            Value = value.Trim().ToUpperInvariant();
        }

        public override string ToString() => Value;
    }
}
