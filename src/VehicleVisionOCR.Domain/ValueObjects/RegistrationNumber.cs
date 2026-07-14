using System;

namespace VehicleVisionOCR.Domain.ValueObjects
{
    /// <summary>
    /// Value object representing a vehicle's registration number (license plate).
    /// </summary>
    public record RegistrationNumber
    {
        public string Value { get; init; }

        public RegistrationNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Registration number cannot be empty.", nameof(value));

            Value = value.Trim().ToUpperInvariant();
        }

        public override string ToString() => Value;
    }
}
