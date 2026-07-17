using System;
using VehicleVisionOCR.Application.Vision.ArtifactRecovery.Interfaces;
using VehicleVisionOCR.Domain.Vision.ArtifactRecovery;

namespace VehicleVisionOCR.Application.Vision.ArtifactRecovery
{
    public class GeometricAnalyzer : IGeometricAnalyzer
    {
        public double CalculateBaselineDeviation(CharacterNode node, RecoveryContext context)
        {
            if (context.MedianY == 0) return 0;
            return Math.Abs(node.Y - context.MedianY) / context.MedianY;
        }

        public double CalculateSpacing(CharacterNode left, CharacterNode right)
        {
            if (left == null || right == null) return 0;
            return Math.Max(0, right.X - (left.X + left.Width));
        }

        public bool IsGeometricOutlier(CharacterNode node, RecoveryContext context)
        {
            if (context.MedianWidth <= 0 || context.MedianHeight <= 0) return false;

            double widthRatio = (double)node.Width / context.MedianWidth;
            double heightRatio = (double)node.Height / context.MedianHeight;
            double baselineDeviation = CalculateBaselineDeviation(node, context);

            // True if it's unusually small (noise) or vastly deviated from baseline
            return (widthRatio < 0.5 && heightRatio < 0.5) || baselineDeviation > context.Config.MaxBaselineDeviationPercentage;
        }
    }
}
