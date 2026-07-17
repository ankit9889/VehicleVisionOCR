namespace VehicleVisionOCR.Domain.VIN.Models
{
    public class VinReasoningConfig
    {
        public bool Allow16CharAsianChassis { get; set; } = true;
        
        /// <summary>
        /// If false, a failed check digit lowers the score but doesn't immediately disqualify the VIN.
        /// Essential for European VINs which often do not enforce ISO 3779 Position 9 check digits.
        /// </summary>
        public bool StrictCheckDigitValidation { get; set; } = false;
        
        public double MinimumAcceptableScore { get; set; } = 60.0;
        
        public ScoringWeights ScoringWeights { get; set; } = new ScoringWeights();
    }
}
