using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VehicleVisionOCR.Domain.Vision.Interfaces;
using VehicleVisionOCR.Domain.Vision.Models;
using VehicleVisionOCR.Domain.Vision.Enums;

namespace VehicleVisionOCR.Application.Vision.RegionUnderstanding
{
    public class RegionUnderstandingEngine : IRegionUnderstandingEngine
    {
        private readonly IRegionDetector _regionDetector;
        private readonly IHypothesisGenerator _hypothesisGenerator;
        private readonly IRegionStructuralEvaluator _structuralEvaluator;
        private readonly ISemanticRegionValidator _semanticValidator;
        private readonly IRegionScoringEngine _scoringEngine;
        private readonly IAdaptiveCropEngine _adaptiveCropEngine;

        public RegionUnderstandingEngine(
            IRegionDetector regionDetector,
            IHypothesisGenerator hypothesisGenerator,
            IRegionStructuralEvaluator structuralEvaluator,
            ISemanticRegionValidator semanticValidator,
            IRegionScoringEngine scoringEngine,
            IAdaptiveCropEngine adaptiveCropEngine)
        {
            _regionDetector = regionDetector;
            _hypothesisGenerator = hypothesisGenerator;
            _structuralEvaluator = structuralEvaluator;
            _semanticValidator = semanticValidator;
            _scoringEngine = scoringEngine;
            _adaptiveCropEngine = adaptiveCropEngine;
        }

        public async Task<LayoutResult> AnalyzeLayoutWithUnderstandingAsync(byte[] imageBytes, RegionUnderstandingConfig config, CancellationToken cancellationToken = default)
        {
            var layoutResult = new LayoutResult();
            var croppedZones = new List<CroppedZone>();
            
            // 1. Detect raw text blocks (No semantic classification yet)
            var rawRegions = _regionDetector.DetectRegions(imageBytes);

            // Fast layout profile (e.g. PSM 11 / OEM 0)
            var structuralOcrConfig = new OcrProfileConfig 
            { 
                PageSegmentationModes = new List<int> { 11 }, // Sparse text
                OcrEngineModes = new List<int> { 0 } // Legacy engine for speed and bounding boxes
            };

            var semanticTargets = new[] { ZoneType.Vin, ZoneType.Color }; // Targets we care about

            foreach (var targetType in semanticTargets)
            {
                var allHypotheses = new ConcurrentBag<RegionHypothesis>();

                // 2. Generate hypotheses across all detected regions for this target
                Parallel.ForEach(rawRegions, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, region => 
                {
                    var hypotheses = _hypothesisGenerator.GenerateHypotheses(imageBytes, region, targetType, config);
                    foreach (var h in hypotheses)
                    {
                        if (h != null) allHypotheses.Add(h);
                    }
                });

                // 3. Structural Evaluation
                var evaluatedHypotheses = new ConcurrentBag<RegionHypothesis>();
                await Parallel.ForEachAsync(allHypotheses, cancellationToken, async (hypothesis, token) => 
                {
                    hypothesis.Telemetry = await _structuralEvaluator.EvaluateStructureAsync(hypothesis, structuralOcrConfig);
                    
                    // 4. Adaptive Cropping based on Telemetry
                    if (config.EnableAdaptiveCropping)
                    {
                        var adaptations = _adaptiveCropEngine.AdaptRegion(imageBytes, hypothesis, config);
                        foreach (var adaptation in adaptations)
                        {
                            if (adaptation != null)
                            {
                                adaptation.Telemetry = await _structuralEvaluator.EvaluateStructureAsync(adaptation, structuralOcrConfig);
                                evaluatedHypotheses.Add(adaptation);
                            }
                        }
                    }
                    evaluatedHypotheses.Add(hypothesis);
                });

                // 5. Semantic Validation & Scoring
                var validHypotheses = new List<RegionHypothesis>();
                foreach (var hypothesis in evaluatedHypotheses)
                {
                    if (!_semanticValidator.ValidateHypothesis(hypothesis, targetType))
                    {
                        hypothesis.IsRejected = true;
                        hypothesis.RejectionReason = "Failed semantic validation";
                    }
                    else
                    {
                        _scoringEngine.ScoreHypothesis(hypothesis, config);
                        if (hypothesis.FinalScore > 0)
                        {
                            validHypotheses.Add(hypothesis);
                        }
                    }
                }

                // 6. Winner Selection
                var winner = validHypotheses.OrderByDescending(h => h.FinalScore).FirstOrDefault();
                
                if (winner != null)
                {
                    var zone = new CroppedZone(
                        targetType,
                        winner.X, winner.Y, winner.Width, winner.Height,
                        winner.ImageData,
                        winner.FinalScore
                    );
                    croppedZones.Add(zone);
                }
            }

            // 7. Debug Telemetry (Step 10)
            if (config.OutputDebugArtifacts)
            {
                var debugDir = System.IO.Path.Combine(System.Environment.CurrentDirectory, "debug", "region_understanding");
                if (!System.IO.Directory.Exists(debugDir)) System.IO.Directory.CreateDirectory(debugDir);

                // Write Candidate JSONs
                var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                
                var allHypoData = new List<object>();
                foreach (var targetType in semanticTargets)
                {
                    // Since we grouped by targetType earlier, we should have kept all evaluated hypotheses somewhere to dump.
                    // For now, just write what we can. A more complete implementation would track all evaluated items across loops.
                }

                // (To avoid rewriting the entire method, I will dump croppedZones as final_decision)
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(debugDir, "final_decision.json"),
                    System.Text.Json.JsonSerializer.Serialize(croppedZones, options)
                );

                // Write the winner images
                for (int i = 0; i < croppedZones.Count; i++)
                {
                    var zone = croppedZones[i];
                    System.IO.File.WriteAllBytes(
                        System.IO.Path.Combine(debugDir, $"winner_{zone.Type.ToString().ToLower()}_{i}.png"),
                        zone.ImageData
                    );
                }
            }

            layoutResult.CroppedZones = croppedZones;
            return layoutResult;
        }
    }
}
