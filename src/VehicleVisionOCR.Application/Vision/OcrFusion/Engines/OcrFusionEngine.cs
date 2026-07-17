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
        private readonly VehicleVisionOCR.Application.Vision.ArtifactRecovery.Interfaces.IOcrArtifactRecoveryEngine _recoveryEngine;

        public OcrFusionEngine(
            IZoneOcrRunner ocrRunner,
            IOcrCandidateCollector candidateCollector,
            ICharacterVotingEngine votingEngine,
            VehicleVisionOCR.Application.Vision.ArtifactRecovery.Interfaces.IOcrArtifactRecoveryEngine recoveryEngine)
        {
            _ocrRunner = ocrRunner;
            _candidateCollector = candidateCollector;
            _votingEngine = votingEngine;
            _recoveryEngine = recoveryEngine;
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

            // 4. Artifact Recovery (Build nodes from clusters and alternatives)
            var recoveryNodes = new System.Collections.Generic.List<VehicleVisionOCR.Domain.Vision.ArtifactRecovery.CharacterNode>();
            int idx = 0;
            foreach (var cluster in clusters)
            {
                var evidence = cluster.EvidenceList.OrderByDescending(e => e.Confidence).FirstOrDefault();
                if (evidence != null && idx < fusedCandidate.Text.Length)
                {
                    recoveryNodes.Add(new VehicleVisionOCR.Domain.Vision.ArtifactRecovery.CharacterNode
                    {
                        PrimaryChar = fusedCandidate.Text[idx],
                        Confidence = fusedCandidate.CharacterConfidences.Count > idx ? fusedCandidate.CharacterConfidences[idx].Confidence : 0,
                        X = cluster.BoundingBoxX,
                        Y = cluster.BoundingBoxY,
                        Width = cluster.BoundingBoxWidth,
                        Height = cluster.BoundingBoxHeight,
                        Alternatives = evidence.Alternatives
                    });
                }
                idx++;
            }

            var recoveryResult = _recoveryEngine.ProcessSequence(recoveryNodes, new VehicleVisionOCR.Domain.Vision.ArtifactRecovery.OcrRecoveryOptions());

            if (System.IO.Directory.Exists(debugDir))
            {
                var rpt = new System.Text.StringBuilder();
                rpt.AppendLine("--- ARTIFACT RECOVERY ---");
                rpt.AppendLine($"Original: {recoveryResult.OriginalText}");
                rpt.AppendLine($"Corrected: {recoveryResult.CorrectedText}");
                foreach (var rep in recoveryResult.AppliedRepairs)
                {
                    rpt.AppendLine($"- {rep.RepairType}: {rep.MathematicalReason}");
                }
                System.IO.File.AppendAllText(System.IO.Path.Combine(debugDir, "ocr_trace.txt"), rpt.ToString() + "\n");
            }

            sw.Stop();

            // 5. Construct Final Result
            return new FusionResult
            {
                Region = zone.Type,
                WinningText = recoveryResult.CorrectedText,
                Confidence = recoveryResult.CorrectedConfidence,
                CharacterEvidenceClusters = clusters,
                TotalExecutionTime = sw.Elapsed,
                // These would normally be derived from the highest scoring individual observation
                WinningOcrConfiguration = "Fused-Ensemble",
                WinningPreprocessing = "Fused-Ensemble"
            };
        }
    }
}
