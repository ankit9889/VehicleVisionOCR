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
            if (node.Confidence < 90 && node.Alternatives != null)
            {
                foreach (var alt in node.Alternatives)
                {
                    if (IsHighlyConfusable(node.PrimaryChar, alt.Character) && alt.Confidence > 30) // if alt confidence is somewhat reasonable
                    {
                        action = new RepairAction
                        {
                            Type = ArtifactRepairType.GeometricSubstitution,
                            ConfidencePenalty = 0,
                            Reason = $"Substituted '{node.PrimaryChar}' with geometrically similar '{alt.Character}' due to low primary confidence and viable alternative.",
                            ModifiedNodes = new List<CharacterNode>
                            {
                                new CharacterNode
                                {
                                    PrimaryChar = alt.Character,
                                    Confidence = alt.Confidence,
                                    X = node.X,
                                    Y = node.Y,
                                    Width = node.Width,
                                    Height = node.Height,
                                    Alternatives = node.Alternatives,
                                    SourceReference = node.SourceReference
                                }
                            }
                        };
                        return true;
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
