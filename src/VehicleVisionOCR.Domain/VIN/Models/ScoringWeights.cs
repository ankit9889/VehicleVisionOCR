namespace VehicleVisionOCR.Domain.VIN.Models
{
    public class ScoringWeights
    {
        public double CheckDigit { get; set; } = 25.0;
        public double Length { get; set; } = 20.0;
        public double Regex { get; set; } = 20.0;
        public double Manufacturer { get; set; } = 15.0;
        public double SerialStructure { get; set; } = 10.0;
        public double OcrConfidence { get; set; } = 10.0;
    }
}
