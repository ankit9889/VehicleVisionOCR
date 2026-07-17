using VehicleVisionOCR.Domain.Vision.ArtifactRecovery;

namespace VehicleVisionOCR.Application.Vision.ArtifactRecovery.Interfaces
{
    public interface IGeometricAnalyzer
    {
        double CalculateBaselineDeviation(CharacterNode node, RecoveryContext context);
        double CalculateSpacing(CharacterNode left, CharacterNode right);
        bool IsGeometricOutlier(CharacterNode node, RecoveryContext context);
    }
}
