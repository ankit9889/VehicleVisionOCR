namespace VehicleVisionOCR.Domain.VIN.Models
{
    public class RuleViolation
    {
        public string RuleName { get; set; }
        public string Description { get; set; }
        public double PointsDeducted { get; set; }
    }
}
