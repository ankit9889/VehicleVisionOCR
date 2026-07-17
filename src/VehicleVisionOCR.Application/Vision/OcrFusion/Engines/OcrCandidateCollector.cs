using System.Collections.Generic;
using System.Linq;
using VehicleVisionOCR.Domain.Vision.Interfaces;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Application.Vision.OcrFusion.Engines
{
    public class OcrCandidateCollector : IOcrCandidateCollector
    {
        public List<CharacterCluster> ClusterObservations(List<OcrObservation> rawPasses)
        {
            var allEvidence = new List<CharacterEvidence>();
            foreach (var pass in rawPasses)
            {
                if (pass.Characters != null)
                {
                    allEvidence.AddRange(pass.Characters);
                }
            }

            if (!allEvidence.Any()) return new List<CharacterCluster>();

            // Sort all observed characters from left to right across all passes
            allEvidence = allEvidence.OrderBy(e => e.X).ToList();

            var clusters = new List<CharacterCluster>();
            
            foreach (var evidence in allEvidence)
            {
                bool addedToExisting = false;

                // Try to find an existing cluster that this character belongs to.
                // Characters belong to the same cluster if they significantly overlap horizontally.
                foreach (var cluster in clusters)
                {
                    if (OverlapsHorizontally(evidence, cluster) && OverlapsVertically(evidence, cluster))
                    {
                        cluster.EvidenceList.Add(evidence);
                        
                        // Expand cluster boundaries
                        cluster.BoundingBoxX = System.Math.Min(cluster.BoundingBoxX, evidence.X);
                        cluster.BoundingBoxY = System.Math.Min(cluster.BoundingBoxY, evidence.Y);
                        cluster.BoundingBoxWidth = System.Math.Max(cluster.BoundingBoxX + cluster.BoundingBoxWidth, evidence.X + evidence.Width) - cluster.BoundingBoxX;
                        cluster.BoundingBoxHeight = System.Math.Max(cluster.BoundingBoxY + cluster.BoundingBoxHeight, evidence.Y + evidence.Height) - cluster.BoundingBoxY;
                        
                        addedToExisting = true;
                        break; // Move to next evidence
                    }
                }

                if (!addedToExisting)
                {
                    // Start a new cluster
                    var newCluster = new CharacterCluster
                    {
                        BoundingBoxX = evidence.X,
                        BoundingBoxY = evidence.Y,
                        BoundingBoxWidth = evidence.Width,
                        BoundingBoxHeight = evidence.Height,
                    };
                    newCluster.EvidenceList.Add(evidence);
                    clusters.Add(newCluster);
                }
            }

            // Assign positional index after clusters are finalized and sorted (Y first to separate lines, then X)
            clusters = clusters.OrderBy(c => c.BoundingBoxY / 15).ThenBy(c => c.BoundingBoxX).ToList();
            for (int i = 0; i < clusters.Count; i++)
            {
                clusters[i].PositionIndex = i;
            }

            return clusters;
        }

        private bool OverlapsHorizontally(CharacterEvidence evidence, CharacterCluster cluster)
        {
            // Calculate horizontal intersection
            int intersectLeft = System.Math.Max(evidence.X, cluster.BoundingBoxX);
            int intersectRight = System.Math.Min(evidence.X + evidence.Width, cluster.BoundingBoxX + cluster.BoundingBoxWidth);
            
            int intersectWidth = intersectRight - intersectLeft;

            if (intersectWidth <= 0) return false;

            // If it overlaps by at least 50% of the character's width, consider it the same positional cluster
            double overlapRatio = (double)intersectWidth / System.Math.Min(evidence.Width, cluster.BoundingBoxWidth);
            return overlapRatio > 0.5;
        }

        private bool OverlapsVertically(CharacterEvidence evidence, CharacterCluster cluster)
        {
            // Calculate vertical intersection
            int intersectTop = System.Math.Max(evidence.Y, cluster.BoundingBoxY);
            int intersectBottom = System.Math.Min(evidence.Y + evidence.Height, cluster.BoundingBoxY + cluster.BoundingBoxHeight);
            
            int intersectHeight = intersectBottom - intersectTop;

            if (intersectHeight <= 0) return false;

            // If it overlaps by at least 50% of the character's height, consider it the same line cluster
            double overlapRatio = (double)intersectHeight / System.Math.Min(evidence.Height, cluster.BoundingBoxHeight);
            return overlapRatio > 0.5;
        }
    }
}
