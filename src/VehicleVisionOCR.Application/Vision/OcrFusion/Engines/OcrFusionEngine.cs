using System;
using System.Diagnostics;
using System.Threading.Tasks;
using VehicleVisionOCR.Domain.Vision.Interfaces;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Application.Vision.OcrFusion.Engines
{
    public class OcrFusionEngine : IOcrFusionEngine
    {
        private readonly IZoneOcrRunner _ocrRunner;
        private readonly IOcrCandidateCollector _candidateCollector;
        private readonly ICharacterVotingEngine _votingEngine;

        public OcrFusionEngine(
            IZoneOcrRunner ocrRunner,
            IOcrCandidateCollector candidateCollector,
            ICharacterVotingEngine votingEngine)
        {
            _ocrRunner = ocrRunner;
            _candidateCollector = candidateCollector;
            _votingEngine = votingEngine;
        }

        public async Task<FusionResult> ProcessZoneAsync(CroppedZone zone, OcrProfileConfig config)
        {
            var sw = Stopwatch.StartNew();

            // 1. Run all OCR passes in parallel across different scales and PSMs
            var observations = await _ocrRunner.RunOcrPassesAsync(zone.ImageData, config);

            // Debug OCR Trace
            var debugDir = System.IO.Path.Combine(System.Environment.CurrentDirectory, "debug");
            if (System.IO.Directory.Exists(debugDir))
            {
                var traceBuilder = new System.Text.StringBuilder();
                traceBuilder.AppendLine("================ OCR TRACE ================");
                traceBuilder.AppendLine($"Region Name: {zone.Type}");
                traceBuilder.AppendLine($"Crop Size: {zone.Width}x{zone.Height}");
                traceBuilder.AppendLine($"Bounding Box: ({zone.X}, {zone.Y})");
                traceBuilder.AppendLine($"OCR Configuration: PSM {string.Join(",", config.PageSegmentationModes)} | Engine {string.Join(",", config.OcrEngineModes)}");
                
                int passNum = 1;
                foreach (var obs in observations)
                {
                    traceBuilder.AppendLine($"\n--- Pass {passNum++} ---");
                    traceBuilder.AppendLine($"Preprocessing: {(string.IsNullOrEmpty(obs.PreprocessingMethod) ? "None" : obs.PreprocessingMethod)}");
                    traceBuilder.AppendLine($"OCR Text: {(obs.RawText != null ? obs.RawText.Replace("\n", "\\n").Replace("\r", "") : "")}");
                    traceBuilder.AppendLine($"Character Count: {(obs.Characters != null ? obs.Characters.Count : 0)}");
                    traceBuilder.AppendLine($"Average Confidence: {obs.AverageConfidence:F2}");
                }
                
                System.IO.File.AppendAllText(System.IO.Path.Combine(debugDir, "ocr_trace.txt"), traceBuilder.ToString() + "\n");
            }

            // 2. Geometrically cluster the bounding boxes of every observed character
            var clusters = _candidateCollector.ClusterObservations(observations);

            // 3. Perform mathematical probability voting on the clusters
            var fusedCandidate = _votingEngine.VoteOnClusters(clusters);

            sw.Stop();

            // 4. Construct Final Result
            return new FusionResult
            {
                Region = zone.Type,
                WinningText = fusedCandidate.Text,
                Confidence = fusedCandidate.AggregateConfidence,
                CharacterEvidenceClusters = clusters,
                TotalExecutionTime = sw.Elapsed,
                // These would normally be derived from the highest scoring individual observation
                WinningOcrConfiguration = "Fused-Ensemble",
                WinningPreprocessing = "Fused-Ensemble"
            };
        }
    }
}
