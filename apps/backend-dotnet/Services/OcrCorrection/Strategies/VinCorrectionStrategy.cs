using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Enums;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Helpers;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Models;

namespace VehicleVisionOCR.Backend.Services.OcrCorrection.Strategies
{
    /// <summary>
    /// Implementation of <see cref="IOcrCorrectionStrategy"/> specifically tailored to Vehicle Identification Numbers.
    /// Acts as an orchestrator for normalizers, candidate generators, and scoring services.
    /// </summary>
    public class VinCorrectionStrategy : IOcrCorrectionStrategy
    {
        private readonly IVinNormalizer _normalizer;
        private readonly IVinCandidateGenerator _candidateGenerator;
        private readonly IVinScoringService _scorer;
        private readonly IWmiRepository _wmiRepository;
        private readonly IMemoryCache _cache;
        private readonly OcrCorrectionOptions _options;

        /// <inheritdoc/>
        public TargetFieldType FieldType => TargetFieldType.VIN;

        /// <summary>
        /// Initializes a new instance of the <see cref="VinCorrectionStrategy"/> class.
        /// </summary>
        public VinCorrectionStrategy(
            IVinNormalizer normalizer,
            IVinCandidateGenerator candidateGenerator,
            IVinScoringService scorer,
            IWmiRepository wmiRepository,
            IMemoryCache cache,
            IOptions<OcrCorrectionOptions> options)
        {
            _normalizer = normalizer;
            _candidateGenerator = candidateGenerator;
            _scorer = scorer;
            _wmiRepository = wmiRepository;
            _cache = cache;
            _options = options.Value;
        }

        /// <inheritdoc/>
        public async Task<CorrectionResult> CorrectAsync(string rawText, double ocrConfidence)
        {
            if (string.IsNullOrWhiteSpace(rawText))
                return CreateFailedResult(rawText, "Empty text");

            // 1. Universal Normalization
            var (baseCandidate, universalRules) = _normalizer.NormalizeUniversalRules(rawText);

            // 2. Generate Candidate Pool
            var candidates = await _candidateGenerator.GenerateCandidatesAsync(baseCandidate);

            // 3. Apply structural normalizations to all candidates
            foreach (var c in candidates)
            {
                var (structNorm, structRules) = _normalizer.NormalizeStructuralRules(c.Candidate);
                c.Candidate = structNorm;
                c.Rules.AddRange(structRules);
            }

            // Fetch WMI Cache for Scoring
            var knownWmis = await _cache.GetOrCreateAsync("WmiPrefixes", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = System.TimeSpan.FromMinutes(_options.CacheExpirationMinutes);
                return (await _wmiRepository.GetActiveWmiPrefixesAsync()).ToList();
            });

            // 4. Score all candidates
            var scoredCandidates = candidates.Select(c => new
            {
                Candidate = c.Candidate,
                Rules = universalRules.Concat(c.Rules).ToList(),
                Score = _scorer.ScoreCandidate(c.Candidate, rawText, ocrConfidence, knownWmis)
            }).OrderByDescending(x => x.Score).ToList();

            var best = scoredCandidates.FirstOrDefault();

            if (best == null)
                return CreateFailedResult(rawText, "No valid candidates generated.");

            var result = new CorrectionResult
            {
                OriginalText = rawText,
                StrategyName = nameof(VinCorrectionStrategy),
                CorrectedText = best.Candidate,
                WasCorrected = (best.Candidate != rawText),
                AppliedRules = best.Rules,
                FinalScore = best.Score,
                ConfidenceLevel = DetermineConfidenceLevel(best.Score)
            };

            if (best.Candidate.Length != 17)
            {
                result.IsValid = false;
                result.FailureReason = $"Invalid length ({best.Candidate.Length} chars). Expected 17.";
                return result;
            }

            if (best.Score < _options.MinVinScoreThreshold)
            {
                result.IsValid = false;
                result.FailureReason = $"Score ({best.Score}) below minimum threshold ({_options.MinVinScoreThreshold})";
                return result;
            }

            if (!VinCheckDigitCalculator.Validate(best.Candidate))
            {
                result.IsValid = false;
                result.FailureReason = "Failed ISO 3779 Modulus 11 Check Digit validation.";
                return result;
            }

            result.IsValid = true;
            return result;
        }

        private CorrectionResult CreateFailedResult(string raw, string reason)
        {
            return new CorrectionResult
            {
                OriginalText = raw,
                CorrectedText = raw,
                IsValid = false,
                FailureReason = reason,
                StrategyName = nameof(VinCorrectionStrategy),
                ConfidenceLevel = ConfidenceLevel.Low
            };
        }

        private ConfidenceLevel DetermineConfidenceLevel(double score)
        {
            if (score >= 85.0) return ConfidenceLevel.High;
            if (score >= _options.MinVinScoreThreshold) return ConfidenceLevel.Medium;
            return ConfidenceLevel.Low;
        }
    }
}
