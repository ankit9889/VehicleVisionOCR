using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using VehicleVisionOCR.Backend.Helpers;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Enums;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Models;

namespace VehicleVisionOCR.Backend.Services.OcrCorrection.Strategies
{
    public partial class ColorCorrectionStrategy : IOcrCorrectionStrategy
    {
        private readonly IColorRepository _colorRepository;
        private readonly IMemoryCache _cache;
        private readonly OcrCorrectionOptions _options;
        private static readonly System.Threading.SemaphoreSlim _colorSemaphore = new System.Threading.SemaphoreSlim(1, 1);
        private readonly Dictionary<char, char> _colorConfusionMap = new()
        {
            {'0', 'O'}, {'1', 'I'}, {'5', 'S'}, {'8', 'B'}, {'2', 'Z'}
        };

        [GeneratedRegex(@"\s+")]
        private static partial Regex WhitespaceRegex();

        public TargetFieldType FieldType => TargetFieldType.Color;

        public ColorCorrectionStrategy(
            IColorRepository colorRepository,
            IMemoryCache cache,
            IOptions<OcrCorrectionOptions> options)
        {
            _colorRepository = colorRepository;
            _cache = cache;
            _options = options.Value;
        }

        public async Task<CorrectionResult> CorrectAsync(string rawText, double ocrConfidence)
        {
            var result = new CorrectionResult
            {
                OriginalText = rawText,
                StrategyName = nameof(ColorCorrectionStrategy)
            };

            if (string.IsNullOrWhiteSpace(rawText))
            {
                result.FailureReason = "Empty OCR Text";
                return result;
            }

            string normalized = Normalize(rawText, result.AppliedRules);

            if (!_cache.TryGetValue("ActiveColors", out List<string> activeColors))
            {
                await _colorSemaphore.WaitAsync();
                try
                {
                    if (!_cache.TryGetValue("ActiveColors", out activeColors))
                    {
                        activeColors = (await _colorRepository.GetActiveColorsAsync()).ToList();
                        _cache.Set("ActiveColors", activeColors, TimeSpan.FromMinutes(_options.CacheExpirationMinutes));
                    }
                }
                finally
                {
                    _colorSemaphore.Release();
                }
            }

            var (bestMatch, matchScore, matchType) = FindBestMatch(normalized, activeColors);

            if (bestMatch != null)
            {
                result.CorrectedText = bestMatch;
                result.WasCorrected = (bestMatch != rawText);
                result.AppliedRules.Add($"Matched via {matchType}");

                double finalScore = (ocrConfidence * 0.3) + (matchScore * 0.7);
                result.FinalScore = finalScore;
                result.ConfidenceLevel = DetermineConfidenceLevel(finalScore);

                if (finalScore >= _options.MinColorScoreThreshold) 
                {
                    result.IsValid = true;
                }
                else
                {
                    result.IsValid = false;
                    result.FailureReason = $"Match composite confidence ({finalScore}%) below threshold ({_options.MinColorScoreThreshold}%)";
                }
            }
            else
            {
                result.CorrectedText = "Unknown";
                result.IsValid = false;
                result.FailureReason = "No acceptable match found in Database.";
            }

            return result;
        }

        private string Normalize(string text, List<string> rules)
        {
            string norm = text.Trim().ToUpperInvariant();
            norm = Regex.Replace(norm, @"[^A-Z0-9 ]+", "");
            norm = WhitespaceRegex().Replace(norm, " "); // Collapse multiple spaces

            char[] chars = norm.ToCharArray();
            bool appliedConfusion = false;
            for (int i = 0; i < chars.Length; i++)
            {
                if (_colorConfusionMap.TryGetValue(chars[i], out char letter))
                {
                    chars[i] = letter;
                    appliedConfusion = true;
                }
            }
            if (appliedConfusion) rules.Add("Reversed OCR Confusion (Numbers -> Letters)");

            return new string(chars);
        }

        private static (string? match, double score, string matchType) FindBestMatch(string normalizedText, List<string> activeColors)
        {
            if (activeColors == null || !activeColors.Any()) return (null, 0, "None");

            var exact = activeColors.FirstOrDefault(c => c == normalizedText);
            if (exact != null) return (exact, 100.0, "Exact Match");

            var contains = activeColors.FirstOrDefault(c => normalizedText.Contains(c) || c.Contains(normalizedText));
            if (contains != null) return (contains, 90.0, "Contains Match");

            var normTokens = normalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var dbColor in activeColors)
            {
                var dbTokens = dbColor.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (normTokens.Intersect(dbTokens).Any()) return (dbColor, 80.0, "Token Match");
            }

            string bestFuzzy = null;
            double bestScore = 0.0;
            foreach (var dbColor in activeColors)
            {
                double similarity = FuzzyMatcher.ComputeJaroWinkler(normalizedText, dbColor);
                if (similarity > bestScore)
                {
                    bestScore = similarity;
                    bestFuzzy = dbColor;
                }
            }

            if (bestFuzzy != null && bestScore >= 0.85) // 85% JaroWinkler similarity threshold
            {
                return (bestFuzzy, bestScore * 100.0, "Jaro-Winkler Similarity Match");
            }

            return (null, 0, "None");
        }

        private ConfidenceLevel DetermineConfidenceLevel(double score)
        {
            if (score >= 85.0) return ConfidenceLevel.High;
            if (score >= _options.MinColorScoreThreshold) return ConfidenceLevel.Medium;
            return ConfidenceLevel.Low;
        }
    }
}
