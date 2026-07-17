using System;
using System.Linq;
using System.Threading.Tasks;
using VehicleVisionOCR.Domain.Vision.Interfaces;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Application.Vision.RegionUnderstanding
{
    public class RegionStructuralEvaluator : IRegionStructuralEvaluator
    {
        private readonly IZoneOcrRunner _ocrRunner;

        public RegionStructuralEvaluator(IZoneOcrRunner ocrRunner)
        {
            _ocrRunner = ocrRunner;
        }

        public async Task<StructuralTelemetry> EvaluateStructureAsync(RegionHypothesis hypothesis, OcrProfileConfig ocrConfig)
        {
            // Execute a fast structural OCR pass
            var passes = await _ocrRunner.RunOcrPassesAsync(hypothesis.ImageData, ocrConfig);
            var bestPass = passes.OrderByDescending(p => p.AverageConfidence).FirstOrDefault();

            var telemetry = new StructuralTelemetry();

            if (bestPass == null || bestPass.Characters == null || !bestPass.Characters.Any())
            {
                return telemetry;
            }

            telemetry.RawStructuralText = bestPass.RawText;
            telemetry.OverallLayoutConfidence = bestPass.AverageConfidence;
            telemetry.CharacterCount = bestPass.Characters.Count;

            // Calculate Lines
            var yCoords = bestPass.Characters.Select(c => (double)c.Y).ToList();
            yCoords.Sort();
            
            telemetry.AverageCharacterHeight = bestPass.Characters.Average(c => c.Height);
            
            int lines = 1;
            for (int i = 1; i < yCoords.Count; i++)
            {
                if (yCoords[i] - yCoords[i - 1] > telemetry.AverageCharacterHeight)
                {
                    lines++;
                }
            }
            telemetry.LineCount = lines;

            // Calculate Border Proximity (how close is the text to the edge of the crop)
            double minX = bestPass.Characters.Min(c => c.X);
            double minY = bestPass.Characters.Min(c => c.Y);
            double maxX = bestPass.Characters.Max(c => c.X + c.Width);
            double maxY = bestPass.Characters.Max(c => c.Y + c.Height);

            double borderLeft = minX;
            double borderTop = minY;
            double borderRight = hypothesis.Width - maxX;
            double borderBottom = hypothesis.Height - maxY;
            
            telemetry.BorderProximity = Math.Min(Math.Min(borderLeft, borderTop), Math.Min(borderRight, borderBottom));
            
            // For stability we can mock baseline and spacing
            telemetry.BaselineConsistency = 1.0; 
            telemetry.CharacterSpacingUniformity = 1.0; 

            return telemetry;
        }
    }
}
