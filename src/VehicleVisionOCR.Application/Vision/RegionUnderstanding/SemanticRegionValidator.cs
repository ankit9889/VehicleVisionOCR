using System.Text.RegularExpressions;
using VehicleVisionOCR.Domain.Vision.Interfaces;
using VehicleVisionOCR.Domain.Vision.Models;
using VehicleVisionOCR.Domain.Vision.Enums;

namespace VehicleVisionOCR.Application.Vision.RegionUnderstanding
{
    public class SemanticRegionValidator : ISemanticRegionValidator
    {
        public bool ValidateHypothesis(RegionHypothesis hypothesis, ZoneType targetType)
        {
            var text = hypothesis.Telemetry.RawStructuralText ?? string.Empty;
            
            if (targetType == ZoneType.Vin)
            {
                // Reject if obviously not a VIN
                if (hypothesis.Telemetry.LineCount > 2) return false;
                
                // If there are many lowercase characters, probably an English sentence
                int lowercaseCount = 0;
                foreach (char c in text)
                {
                    if (char.IsLower(c)) lowercaseCount++;
                }
                if (lowercaseCount > 5) return false;
                
                // Reject if lots of spaces (VIN should be one solid block usually)
                int spaces = text.Length - text.Replace(" ", "").Length;
                if (spaces > 3) return false;
            }
            else if (targetType == ZoneType.Color)
            {
                if (hypothesis.Telemetry.LineCount > 3) return false;
                
                // Color regions usually have dictionary words, but let's not reject too aggressively here
            }

            return true;
        }
    }
}
