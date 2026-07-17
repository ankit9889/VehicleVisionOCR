using System.Collections.Generic;
using System.Linq;
using VehicleVisionOCR.Domain.Vision.Enums;
using VehicleVisionOCR.Domain.Vision.Interfaces;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Infrastructure.Vision.OpenCV
{
    public class RegionClassifier : IRegionClassifier
    {
        public void ClassifyRegions(List<RegionCandidate> candidates)
        {
            if (candidates == null || !candidates.Any()) return;

            // 1. Assign Barcode directly from features
            var barcode = candidates.FirstOrDefault(c => c.Features.IsBarcode);
            if (barcode != null)
            {
                barcode.AssignedZone = ZoneType.Barcode;
                barcode.ClassificationConfidence = 0.98;
                barcode.Reasoning = "Matches morphological barcode density profile.";
            }

            // Filter to only unassigned text blocks
            var textBlocks = candidates.Where(c => c.AssignedZone == ZoneType.Unknown).ToList();
            if (!textBlocks.Any()) return;

            // 2. VIN Detection Strategy
            // The VIN is usually the largest/widest text block, often located centrally or directly above/below the barcode.
            var vinCandidate = textBlocks
                .OrderByDescending(c => c.Features.Area * (c.Features.AspectRatio > 3.0 ? 1.5 : 1.0)) // Reward wide blocks
                .FirstOrDefault();

            if (vinCandidate != null)
            {
                vinCandidate.AssignedZone = ZoneType.Vin;
                vinCandidate.ClassificationConfidence = 0.90;
                vinCandidate.Reasoning = "Largest text block with horizontal alignment characteristic of a 17-char string.";
            }

            // 3. Model & Color Detection Strategy
            // Color is often below the barcode or near the VIN.
            // For now, we will assign 'PrimaryText' and 'SecondaryText' to the next largest blocks,
            // as true semantic differentiation of Model vs Color requires OCR Semantic Fusion.
            var remainingBlocks = textBlocks.Where(c => c.AssignedZone == ZoneType.Unknown)
                                            .OrderByDescending(c => c.Features.Area).ToList();
            
            if (remainingBlocks.Count > 0)
            {
                remainingBlocks[0].AssignedZone = ZoneType.PrimaryText;
                remainingBlocks[0].ClassificationConfidence = 0.70;
                remainingBlocks[0].Reasoning = "Second largest text block, probable Model or Color.";
            }
            if (remainingBlocks.Count > 1)
            {
                remainingBlocks[1].AssignedZone = ZoneType.SecondaryText;
                remainingBlocks[1].ClassificationConfidence = 0.60;
                remainingBlocks[1].Reasoning = "Third largest text block, probable Model or Color.";
            }
        }
    }
}
