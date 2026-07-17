using System.Collections.Generic;
using VehicleVisionOCR.Application.Vision.ArtifactRecovery.Interfaces;
using VehicleVisionOCR.Domain.Vision.ArtifactRecovery;

namespace VehicleVisionOCR.Application.Vision.ArtifactRecovery.Strategies
{
    public class DuplicateSuppressionStrategy : IArtifactRepairStrategy
    {
        private readonly IGeometricAnalyzer _analyzer;

        public int ExecutionOrder => 2;

        public DuplicateSuppressionStrategy(IGeometricAnalyzer analyzer)
        {
            _analyzer = analyzer;
        }

        public bool TryRepair(LinkedListNode<CharacterNode> current, RecoveryContext context, out RepairAction action)
        {
            action = null;

            if (current.Next == null) return false;

            var currNode = current.Value;
            var nextNode = current.Next.Value;

            if (currNode.PrimaryChar == nextNode.PrimaryChar)
            {
                // Check bounding box overlap. If they highly overlap, it's OCR stutter.
                double spacing = _analyzer.CalculateSpacing(currNode, nextNode);
                
                // If spacing is negative or extremely close, and they are the same character
                if (spacing <= 2 || currNode.Confidence < 40 || nextNode.Confidence < 40)
                {
                    // Keep the one with higher confidence
                    var survivor = currNode.Confidence >= nextNode.Confidence ? currNode : nextNode;
                    
                    action = new RepairAction
                    {
                        Type = ArtifactRepairType.DuplicateSuppression,
                        ConfidencePenalty = context.Config.ConfidencePenaltyPerRemoval,
                        Reason = $"Suppressed duplicate '{currNode.PrimaryChar}' due to geometric overlap (spacing: {spacing:F1}) or low confidence.",
                        ModifiedNodes = new List<CharacterNode> { survivor } // We replace target with survivor. Engine will remove target and target.Next. (Needs slightly custom handling in Engine, or we just remove the weaker one)
                    };
                    return true;
                }
            }

            return false;
        }
    }
}
