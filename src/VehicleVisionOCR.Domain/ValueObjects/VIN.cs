using System;

namespace VehicleVisionOCR.Domain.ValueObjects
{
    /// <summary>
    /// Value object representing a Vehicle Identification Number (VIN).
    /// </summary>
    public record VIN
    {
        public string Value { get; init; }

        public VIN(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("VIN cannot be empty.", nameof(value));
            
            if (value.Trim().Length < 14 || value.Trim().Length > 20)
                throw new ArgumentException("VIN must be between 14 and 20 characters long.", nameof(value));

            Value = value.Trim().ToUpperInvariant();
        }

        public override string ToString() => Value;
    }
}
