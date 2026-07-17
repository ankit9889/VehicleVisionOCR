using System;
using System.Collections.Generic;
using System.Linq;
using VehicleVisionOCR.Application.Vision.ArtifactRecovery.Interfaces;
using VehicleVisionOCR.Domain.Vision.ArtifactRecovery;

namespace VehicleVisionOCR.Application.Vision.ArtifactRecovery.Strategies
{
    public class SplitCharacterMergeStrategy : IArtifactRepairStrategy
    {
        private readonly IGeometricAnalyzer _analyzer;

        public int ExecutionOrder => 4;

        public SplitCharacterMergeStrategy(IGeometricAnalyzer analyzer)
        {
            _analyzer = analyzer;
        }

        public bool TryRepair(LinkedListNode<CharacterNode> current, RecoveryContext context, out RepairAction action)
        {
            action = null;
            if (current.Next == null) return false;

            var left = current.Value;
            var right = current.Next.Value;

            // If both characters are unusually narrow
            if (context.MedianWidth > 0 && 
                left.Width < context.MedianWidth * 0.7 && 
                right.Width < context.MedianWidth * 0.7)
            {
                // And they are close to each other
                double spacing = _analyzer.CalculateSpacing(left, right);
                if (spacing < context.MedianWidth * 0.2) // very close or overlapping
                {
                    // Check if combining them creates a normal width
                    double combinedWidth = (right.X + right.Width) - left.X;
                    if (Math.Abs(combinedWidth - context.MedianWidth) / context.MedianWidth < 0.3)
                    {
                        // Check if there is a known split pattern, or check Alternatives for a merged character
                        // We can look at left.Alternatives to see if Tesseract suggested a single wide char.
                        string combinedStr = $"{left.PrimaryChar}{right.PrimaryChar}";
                        char? mergedChar = GetKnownSplit(combinedStr);

                        if (mergedChar.HasValue)
                        {
                            var mergedNode = new CharacterNode
                            {
                                PrimaryChar = mergedChar.Value,
                                Confidence = Math.Min(left.Confidence, right.Confidence) * 0.9, // Penalize slightly
                                X = left.X,
                                Y = Math.Min(left.Y, right.Y),
                                Width = (int)combinedWidth,
                                Height = Math.Max(left.Height, right.Height),
                                SourceReference = left.SourceReference
                            };

                            action = new RepairAction
                            {
                                Type = ArtifactRepairType.SplitCharacterMerge,
                                ConfidencePenalty = context.Config.ConfidencePenaltyPerMerge,
                                Reason = $"Merged '{combinedStr}' into '{mergedChar.Value}'. Combined width {(combinedWidth/context.MedianWidth):P0} of median.",
                                ModifiedNodes = new List<CharacterNode> { mergedNode }
                            };
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private char? GetKnownSplit(string pair)
        {
            return pair switch
            {
                "RN" => 'M',
                "VV" => 'W',
                "CL" => 'D',
                "II" => 'H',
                _ => null
            };
        }
    }
}
