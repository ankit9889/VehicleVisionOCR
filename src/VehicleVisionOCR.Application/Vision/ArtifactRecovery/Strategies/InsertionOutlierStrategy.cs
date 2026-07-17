using System.Collections.Generic;
using VehicleVisionOCR.Application.Vision.ArtifactRecovery.Interfaces;
using VehicleVisionOCR.Domain.Vision.ArtifactRecovery;

namespace VehicleVisionOCR.Application.Vision.ArtifactRecovery.Strategies
{
    public class InsertionOutlierStrategy : IArtifactRepairStrategy
    {
        private readonly IGeometricAnalyzer _analyzer;

        public int ExecutionOrder => 3;

        public InsertionOutlierStrategy(IGeometricAnalyzer analyzer)
        {
            _analyzer = analyzer;
        }

        public bool TryRepair(LinkedListNode<CharacterNode> current, RecoveryContext context, out RepairAction action)
        {
            action = null;
            var node = current.Value;

            // If it's a geometric outlier (way too thin/short compared to median) and confidence isn't perfect
            if (_analyzer.IsGeometricOutlier(node, context) && node.Confidence < 70)
            {
                action = new RepairAction
                {
                    Type = ArtifactRepairType.InsertionOutlierRemoval,
                    ConfidencePenalty = context.Config.ConfidencePenaltyPerRemoval,
                    Reason = $"Removed insertion outlier '{node.PrimaryChar}'. Width/Height ratio to median is extreme, confidence {node.Confidence:F1} < 70."
                };
                return true;
            }

            return false;
        }
    }
}
