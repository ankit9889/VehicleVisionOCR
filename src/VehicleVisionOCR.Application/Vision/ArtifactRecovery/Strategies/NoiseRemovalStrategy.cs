using System.Collections.Generic;
using VehicleVisionOCR.Application.Vision.ArtifactRecovery.Interfaces;
using VehicleVisionOCR.Domain.Vision.ArtifactRecovery;

namespace VehicleVisionOCR.Application.Vision.ArtifactRecovery.Strategies
{
    public class NoiseRemovalStrategy : IArtifactRepairStrategy
    {
        private readonly IGeometricAnalyzer _analyzer;
        
        public int ExecutionOrder => 1;

        public NoiseRemovalStrategy(IGeometricAnalyzer analyzer)
        {
            _analyzer = analyzer;
        }

        public bool TryRepair(LinkedListNode<CharacterNode> current, RecoveryContext context, out RepairAction action)
        {
            action = null;
            char c = current.Value.PrimaryChar;

            // Check for typical noise characters
            if (c == '_' || c == '-' || c == '\'' || c == '.' || c == ',' || c == ' ')
            {
                // Verify mathematically - if confidence is low or it's a geometric outlier (very small, etc.)
                if (current.Value.Confidence < context.Config.NoiseConfidenceThreshold || 
                    _analyzer.IsGeometricOutlier(current.Value, context))
                {
                    action = new RepairAction
                    {
                        Type = ArtifactRepairType.NoiseRemoval,
                        ConfidencePenalty = context.Config.ConfidencePenaltyPerRemoval,
                        Reason = $"Removed noise character '{c}' due to low confidence ({current.Value.Confidence:F1}) or geometric outlier status."
                    };
                    return true;
                }
            }

            return false;
        }
    }
}
