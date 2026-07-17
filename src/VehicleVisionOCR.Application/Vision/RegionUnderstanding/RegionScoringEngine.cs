using System;
using VehicleVisionOCR.Domain.Vision.Interfaces;
using VehicleVisionOCR.Domain.Vision.Models;
using VehicleVisionOCR.Domain.Vision.Enums;

namespace VehicleVisionOCR.Application.Vision.RegionUnderstanding
{
    public class RegionScoringEngine : IRegionScoringEngine
    {
        public void ScoreHypothesis(RegionHypothesis hypothesis, RegionUnderstandingConfig config)
        {
            if (hypothesis.IsRejected)
            {
                hypothesis.FinalScore = 0;
                return;
            }

            // BASE: OCR Confidence is the absolute source of truth
            double baseScore = hypothesis.Telemetry.OverallLayoutConfidence;
            double multiplier = 1.0;

            // 1. Baseline & Spacing Geometry
            multiplier *= (0.90 + (hypothesis.Telemetry.BaselineConsistency * 0.10));
            multiplier *= (0.90 + (hypothesis.Telemetry.CharacterSpacingUniformity * 0.10));

            // 2. Border Proximity Penalty (Risk of truncation)
            if (hypothesis.Telemetry.BorderProximity < 3.0)
            {
                multiplier *= 0.75; 
            }

            // 3. Text Density & Height Uniformity (Assuming these can be approximated if not strictly in telemetry)
            // If AverageCharacterHeight is extremely small relative to box, it's a massive block (Case 1)
            double heightRatio = hypothesis.Telemetry.AverageCharacterHeight / (double)Math.Max(1, hypothesis.Height);
            if (heightRatio < 0.2) multiplier *= 0.60; // Too much whitespace vertically

            // 4. Semantic Validation Multipliers
            if (hypothesis.SemanticTarget == ZoneType.Vin)
            {
                // Line Count penalties
                if (hypothesis.Telemetry.LineCount == 1) multiplier *= 1.15;
                else if (hypothesis.Telemetry.LineCount > 1) multiplier *= 0.10; // Aggressive kill for multi-line

                // Length penalties
                int chars = hypothesis.Telemetry.CharacterCount;
                if (chars >= 15 && chars <= 22) multiplier *= 1.15; // Safe bonus range accommodating 20-char strings
                else if (chars > 25) multiplier *= 0.05; // Massive block kill (Case 1)
                else if (chars < 10) multiplier *= 0.10; // Impossible VIN
            }
            else if (hypothesis.SemanticTarget == ZoneType.Color)
            {
                int chars = hypothesis.Telemetry.CharacterCount;
                if (chars < 5) multiplier *= 0.20;
                else if (chars > 40) multiplier *= 0.50; // Too much text grabbed

                if (hypothesis.Telemetry.LineCount <= 2) multiplier *= 1.10;
                else if (hypothesis.Telemetry.LineCount > 2) multiplier *= 0.40;
            }

            // 5. Final Score Calculation
            double finalScore = baseScore * multiplier;
            
            // Ensure bounds
            hypothesis.FinalScore = Math.Max(0, Math.Min(1.0, finalScore));
        }
    }
}
