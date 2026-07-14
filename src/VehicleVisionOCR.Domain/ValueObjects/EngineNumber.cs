using System;

namespace VehicleVisionOCR.Domain.ValueObjects
{
    /// <summary>
    /// Value object representing a vehicle's engine number.
    /// </summary>
    public record EngineNumber
    {
        public string Value { get; init; }

        public EngineNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Engine number cannot be empty.", nameof(value));

            Value = value.Trim().ToUpperInvariant();
        }

        public override string ToString() => Value;
    }
}
