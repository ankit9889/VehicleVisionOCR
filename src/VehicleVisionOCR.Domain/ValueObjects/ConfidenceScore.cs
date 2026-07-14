using System;

namespace VehicleVisionOCR.Domain.ValueObjects
{
    /// <summary>
    /// Value object representing the confidence score of an OCR extraction.
    /// </summary>
    public record ConfidenceScore
    {
        /// <summary>
        /// Gets the confidence percentage from 0.0 to 100.0.
        /// </summary>
        public double Percentage { get; init; }

        public ConfidenceScore(double percentage)
        {
            if (percentage < 0.0 || percentage > 100.0)
                throw new ArgumentOutOfRangeException(nameof(percentage), "Confidence score must be between 0 and 100.");

            Percentage = percentage;
        }

        public bool IsReliable(double threshold = 75.0) => Percentage >= threshold;

        public override string ToString() => $"{Percentage:F1}%";
    }
}
