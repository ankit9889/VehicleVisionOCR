using System.Collections.Generic;
using VehicleVisionOCR.Domain.Vision.Interfaces;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Application.Vision.OcrFusion.Engines
{
    public class ProbabilityEngine : IProbabilityEngine
    {
        // These could eventually be injected via IOptions<VotingConfig>
        private const double BaseConfidenceWeight = 0.6;
        private const double BoxStabilityWeight = 0.3;
        private const double ConsensusWeight = 0.1;

        public double CalculateCharacterProbability(CharacterEvidence evidence, CharacterCluster parentCluster, int totalPasses)
        {
            if (evidence == null) return 0;

            // 1. Raw OCR Confidence (normalized 0.0 - 1.0 if not already)
            double rawConf = evidence.Confidence > 1.0 ? evidence.Confidence / 100.0 : evidence.Confidence;
            
            // 2. Box Stability (How close is this specific character's bounding box to the average of the cluster?)
            double boxStability = CalculateBoxStability(evidence, parentCluster);

            // 3. Consensus Bonus (Did multiple passes agree on this exact character in this cluster?)
            double consensusRatio = CalculateConsensus(evidence.Character, parentCluster) / (double)totalPasses;

            // Calculate final weighted probability
            double probability = (rawConf * BaseConfidenceWeight) +
                                 (boxStability * BoxStabilityWeight) +
                                 (consensusRatio * ConsensusWeight);

            return probability;
        }

        private double CalculateBoxStability(CharacterEvidence evidence, CharacterCluster cluster)
        {
            if (cluster.BoundingBoxWidth == 0 || cluster.BoundingBoxHeight == 0) return 1.0;

            // Simple intersection over union (IoU) approximation relative to the cluster's bounding box
            // A perfect match with the cluster boundaries yields 1.0
            
            int intersectX = System.Math.Max(evidence.X, cluster.BoundingBoxX);
            int intersectY = System.Math.Max(evidence.Y, cluster.BoundingBoxY);
            int intersectW = System.Math.Min(evidence.X + evidence.Width, cluster.BoundingBoxX + cluster.BoundingBoxWidth) - intersectX;
            int intersectH = System.Math.Min(evidence.Y + evidence.Height, cluster.BoundingBoxY + cluster.BoundingBoxHeight) - intersectY;

            if (intersectW <= 0 || intersectH <= 0) return 0.0;

            double intersectArea = intersectW * intersectH;
            double evidenceArea = evidence.Width * evidence.Height;
            double clusterArea = cluster.BoundingBoxWidth * cluster.BoundingBoxHeight;
            
            double iou = intersectArea / (evidenceArea + clusterArea - intersectArea);
            return iou;
        }

        private int CalculateConsensus(char character, CharacterCluster cluster)
        {
            int count = 0;
            foreach (var ev in cluster.EvidenceList)
            {
                if (ev.Character == character)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
