using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VehicleVisionOCR.Domain.NLP.Interfaces;
using VehicleVisionOCR.Domain.NLP.Models;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Application.NLP.TextInterpretation
{
    public class TextInterpretationEngine : ITextInterpretationEngine
    {
        private readonly ITextNormalizationService _normalizationService;
        private readonly IWordSegmentationService _segmentationService;
        private readonly ITrieDictionaryService _dictionaryService;
        private readonly ISimilarityScoringEngine _scoringEngine;

        public TextInterpretationEngine(
            ITextNormalizationService normalizationService,
            IWordSegmentationService segmentationService,
            ITrieDictionaryService dictionaryService,
            ISimilarityScoringEngine scoringEngine)
        {
            _normalizationService = normalizationService;
            _segmentationService = segmentationService;
            _dictionaryService = dictionaryService;
            _scoringEngine = scoringEngine;
        }

        public Task<InterpretationResult> InterpretAsync(FusedStringCandidate input, InterpretationProfile config)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.Text))
            {
                return Task.FromResult(new InterpretationResult { NormalizedText = string.Empty });
            }

            // 1. Domain-independent structural normalization
            string normalized = _normalizationService.Normalize(input.Text, config);

            // If dictionary matching is off, we just return the normalized string
            if (!config.EnableDictionaryMatching || config.DictionaryTerms == null || !config.DictionaryTerms.Any())
            {
                return Task.FromResult(new InterpretationResult
                {
                    NormalizedText = normalized,
                    Confidence = input.AggregateConfidence,
                    Evidence = "No dictionary matching applied. Returning structural normalization."
                });
            }

            // 2. Dictionary Matching Flow
            _dictionaryService.LoadDictionary(config.DictionaryTerms);

            // 3. Optional: Segment the word if multiple words are expected (simplifying to single phrase matching for now)
            // var segments = _segmentationService.Segment(normalized);

            // 4. Retrieve candidates within distance
            var candidates = _dictionaryService.FindClosestMatches(normalized, config.MaxEditDistance);

            if (!candidates.Any())
            {
                return Task.FromResult(new InterpretationResult
                {
                    NormalizedText = normalized,
                    Confidence = input.AggregateConfidence,
                    Evidence = "No dictionary matches found within acceptable edit distance."
                });
            }

            // 5. Score using Ensembles
            var scores = _scoringEngine.CalculateScores(normalized, candidates);

            var winner = scores.First();

            // 6. Probability Blending
            // Blend original OCR confidence with the algorithmic similarity score
            double blendedConfidence = (input.AggregateConfidence * 0.4) + (winner.FinalBlendedScore * 0.6);

            var result = new InterpretationResult
            {
                NormalizedText = winner.CandidateTerm,
                Confidence = blendedConfidence,
                Alternatives = scores.Skip(1).Take(3).ToList(),
                Evidence = $"Mapped '{normalized}' to '{winner.CandidateTerm}'. " + winner.Evidence
            };

            return Task.FromResult(result);
        }
    }
}
