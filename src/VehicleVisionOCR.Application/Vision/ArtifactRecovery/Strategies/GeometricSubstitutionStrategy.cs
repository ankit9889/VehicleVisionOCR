using System;
using System.Collections.Generic;
using VehicleVisionOCR.Application.Vision.ArtifactRecovery.Interfaces;
using VehicleVisionOCR.Domain.Vision.ArtifactRecovery;

namespace VehicleVisionOCR.Application.Vision.ArtifactRecovery.Strategies
{
    public class GeometricSubstitutionStrategy : IArtifactRepairStrategy
    {
        public int ExecutionOrder => 5;

        public bool TryRepair(LinkedListNode<CharacterNode> current, RecoveryContext context, out RepairAction action)
        {
            action = null;
            var node = current.Value;

            // Simple heuristic: If confidence is moderate, check alternatives.
            if (node.Confidence < 85 && node.Alternatives != null)
            {
                // If there's an alternative that is geometrically known to be easily confused
                foreach (var alt in node.Alternatives)
                {
                    if (IsHighlyConfusable(node.PrimaryChar, alt.Character) && alt.Confidence > node.Confidence - 20)
                    {
                        // In a real generic engine, we'd only do this if it fixes a structural issue 
                        // (like '0' instead of 'O' in a position that requires digits, but this layer is domain-agnostic).
                        // So geometric substitution here might only apply if we have a pure geometric reason.
                        // For now, we skip generic substitutions unless we add language models.
                        // We will return false to avoid mutating without domain knowledge.
                    }
                }
            }

            return false;
        }

        private bool IsHighlyConfusable(char a, char b)
        {
            return (a == 'T' && b == '7') || (a == '7' && b == 'T') ||
                   (a == 'B' && b == '8') || (a == '8' && b == 'B');
        }
    }
}
