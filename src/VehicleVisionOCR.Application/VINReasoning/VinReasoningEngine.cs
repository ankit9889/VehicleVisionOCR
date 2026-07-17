using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using VehicleVisionOCR.Domain.NLP.Models;
using VehicleVisionOCR.Domain.VIN.Interfaces;
using VehicleVisionOCR.Domain.VIN.Models;

namespace VehicleVisionOCR.Application.VINReasoning
{
    public class VinReasoningEngine : IVinReasoningEngine
    {
        private readonly VinCandidateGenerator _generator;
        private readonly VinRuleEngine _ruleEngine;

        public VinReasoningEngine(VinCandidateGenerator generator, VinRuleEngine ruleEngine)
        {
            _generator = generator;
            _ruleEngine = ruleEngine;
        }

        public Task<VinReasoningResult> ReasonAsync(InterpretationResult input, VinReasoningConfig config, System.Threading.CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();

            // 1. Candidate Branching (Branch reality based on OCR confusion)
            var candidates = _generator.GenerateCandidates(input);

            // 2. Parallel Rule Evaluation
            var parallelOptions = new ParallelOptions 
            { 
                MaxDegreeOfParallelism = System.Math.Max(1, Environment.ProcessorCount - 1),
                CancellationToken = cancellationToken
            };

            // We evaluate all branching realities against automotive rules
            Parallel.ForEach(candidates, parallelOptions, candidate => 
            {
                _ruleEngine.Evaluate(candidate, config);
            });

            // 3. Probabilistic Sorting
            var rankedCandidates = candidates
                .OrderByDescending(c => c.Score.TotalScore)
                .ToList();

            sw.Stop();

            // 4. Decision Engine
            var winner = rankedCandidates.FirstOrDefault();
            
            var result = new VinReasoningResult
            {
                ExecutionTime = sw.Elapsed,
                AlternativeCandidates = rankedCandidates.Skip(1).Take(4).ToList()
            };

            if (winner != null && winner.Score.TotalScore >= config.MinimumAcceptableScore)
            {
                result.IsValid = true;
                result.WinningVin = winner.CandidateString;
                result.FinalConfidenceScore = winner.Score.TotalScore;
                result.WinningCandidateDetails = winner;
            }
            else
            {
                result.IsValid = false;
                result.WinningVin = winner?.CandidateString; // Return the best guess anyway for human review
                result.FinalConfidenceScore = winner?.Score.TotalScore ?? 0;
                result.WinningCandidateDetails = winner;
            }

            return Task.FromResult(result);
        }
    }
}
