using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VehicleVisionOCR.Application.Vision.ArtifactRecovery.Interfaces;
using VehicleVisionOCR.Domain.Vision.ArtifactRecovery;

namespace VehicleVisionOCR.Application.Vision.ArtifactRecovery
{
    public class OcrArtifactRecoveryEngine : IOcrArtifactRecoveryEngine
    {
        private readonly IEnumerable<IArtifactRepairStrategy> _strategies;

        public OcrArtifactRecoveryEngine(IEnumerable<IArtifactRepairStrategy> strategies)
        {
            _strategies = strategies.OrderBy(s => s.ExecutionOrder).ToList();
        }

        public RecoveryResult ProcessSequence(IEnumerable<CharacterNode> rawSequence, OcrRecoveryOptions options)
        {
            var result = new RecoveryResult();
            var sequence = new LinkedList<CharacterNode>(rawSequence);

            if (sequence.Count == 0) return result;

            result.OriginalText = GenerateText(sequence);
            result.OriginalConfidence = CalculateAverageConfidence(sequence);

            var context = BuildContext(sequence, options);

            var current = sequence.First;
            while (current != null)
            {
                var nextNode = current.Next;
                bool repaired = false;

                foreach (var strategy in _strategies)
                {
                    if (strategy.TryRepair(current, context, out var action))
                    {
                        ApplyActionToSequence(sequence, current, action);
                        result.AppliedRepairs.Add(new RepairRecord
                        {
                            RepairType = action.Type,
                            ConfidencePenalty = action.ConfidencePenalty,
                            MathematicalReason = action.Reason
                        });
                        
                        // Recalculate medians if structure significantly changed
                        context = BuildContext(sequence, options);
                        repaired = true;
                        
                        // Move back one step if possible, to re-evaluate the neighborhood
                        nextNode = current.Previous ?? sequence.First;
                        break;
                    }
                }

                current = nextNode;
            }

            result.RecoveredSequence = sequence.ToList();
            result.CorrectedText = GenerateText(sequence);
            
            double totalPenalty = result.AppliedRepairs.Sum(r => r.ConfidencePenalty);
            result.CorrectedConfidence = Math.Max(0, result.OriginalConfidence - totalPenalty);

            return result;
        }

        private RecoveryContext BuildContext(LinkedList<CharacterNode> sequence, OcrRecoveryOptions options)
        {
            var nodes = sequence.ToList();
            return new RecoveryContext
            {
                Sequence = sequence,
                Config = options,
                MedianWidth = CalculateMedian(nodes.Select(n => (double)n.Width).ToList()),
                MedianHeight = CalculateMedian(nodes.Select(n => (double)n.Height).ToList()),
                MedianY = CalculateMedian(nodes.Select(n => (double)n.Y).ToList())
            };
        }

        private void ApplyActionToSequence(LinkedList<CharacterNode> sequence, LinkedListNode<CharacterNode> target, RepairAction action)
        {
            if (action.ModifiedNodes == null || !action.ModifiedNodes.Any())
            {
                // Deletion
                sequence.Remove(target);
            }
            else
            {
                // Replacement (e.g. merging two nodes into one)
                var current = target;
                foreach (var newNode in action.ModifiedNodes)
                {
                    sequence.AddBefore(current, newNode);
                }
                
                // If the strategy signaled a merge of target and next, remove them.
                // Assuming strategies handle the removal logic or tell us what to do.
                // For a robust implementation, the strategy should return the exact sequence to replace the target(s).
                // For simplicity, we just assume it replaces `target` and possibly `target.Next`.
                if (action.Type == ArtifactRepairType.SplitCharacterMerge && target.Next != null)
                {
                    sequence.Remove(target.Next);
                }
                sequence.Remove(target);
            }
        }

        private string GenerateText(LinkedList<CharacterNode> sequence)
        {
            var sb = new StringBuilder();
            foreach (var node in sequence)
            {
                sb.Append(node.PrimaryChar);
            }
            return sb.ToString();
        }

        private double CalculateAverageConfidence(LinkedList<CharacterNode> sequence)
        {
            if (!sequence.Any()) return 0;
            return sequence.Average(n => n.Confidence);
        }

        private double CalculateMedian(List<double> values)
        {
            if (!values.Any()) return 0;
            var sorted = values.OrderBy(v => v).ToList();
            int mid = sorted.Count / 2;
            return sorted.Count % 2 != 0 ? sorted[mid] : (sorted[mid] + sorted[mid - 1]) / 2.0;
        }
    }
}
