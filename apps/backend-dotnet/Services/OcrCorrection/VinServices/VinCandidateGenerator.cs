using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Models;
using Microsoft.Extensions.Caching.Memory;
using VehicleVisionOCR.Backend.Helpers;
using Microsoft.Extensions.Options;

namespace VehicleVisionOCR.Backend.Services.OcrCorrection.VinServices
{
    /// <summary>
    /// Implementation of <see cref="IVinCandidateGenerator"/>.
    /// Relies on IMemoryCache and an injected WMI repository to find likely prefix matches for OCR noise.
    /// </summary>
    public class VinCandidateGenerator : IVinCandidateGenerator
    {
        private readonly IWmiRepository _wmiRepository;
        private readonly IMemoryCache _cache;
        private readonly OcrCorrectionOptions _options;
        private static readonly System.Threading.SemaphoreSlim _wmiSemaphore = new System.Threading.SemaphoreSlim(1, 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="VinCandidateGenerator"/> class.
        /// </summary>
        public VinCandidateGenerator(
            IWmiRepository wmiRepository, 
            IMemoryCache cache, 
            IOptions<OcrCorrectionOptions> options)
        {
            _wmiRepository = wmiRepository;
            _cache = cache;
            _options = options.Value;
        }

        private static readonly Dictionary<char, char[]> ConfusionMatrix = new Dictionary<char, char[]>
        {
            {'0', new[] {'O', 'D', 'Q'}},
            {'O', new[] {'0', 'D', 'Q'}},
            {'1', new[] {'I', 'l', 'T', '7'}},
            {'I', new[] {'1', 'l', 'T'}},
            {'5', new[] {'S'}},
            {'S', new[] {'5'}},
            {'8', new[] {'B'}},
            {'B', new[] {'8'}},
            {'6', new[] {'G'}},
            {'G', new[] {'6'}},
            {'2', new[] {'Z'}},
            {'Z', new[] {'2'}},
            {'7', new[] {'T', '1'}},
            {'T', new[] {'7', '1'}},
            {'D', new[] {'0', 'O'}},
            {'Q', new[] {'0', 'O'}},
            {'A', new[] {'4'}},
            {'4', new[] {'A'}}
        };

        /// <inheritdoc/>
        public async Task<List<CandidateScore>> GenerateCandidatesAsync(string normalizedBase)
        {
            var candidates = new List<CandidateScore>();
            if (string.IsNullOrEmpty(normalizedBase)) return candidates;

            char[] current = new char[normalizedBase.Length];
            GenerateCombinations(normalizedBase, 0, current, 0, candidates);

            // WMI Fuzzy matching is still valuable. If we have prefix matches, we could add them,
            // but the confusion matrix already handles character replacement cleanly and globally.
            // We return the combinatorial candidates here. Scoring will handle WMI validation.
            
            return candidates;
        }

        private void GenerateCombinations(string original, int index, char[] current, int substitutions, List<CandidateScore> results)
        {
            // Limit combinatorial explosion
            if (substitutions > 4) return;
            if (results.Count >= 2000) return;

            if (index == original.Length)
            {
                results.Add(new CandidateScore 
                { 
                    Candidate = new string(current),
                    Rules = substitutions > 0 ? new List<string> { $"Confusion Matrix ({substitutions} changes)" } : new List<string>(),
                    Substitutions = substitutions
                });
                return;
            }

            // Path 1: Keep original character
            current[index] = original[index];
            GenerateCombinations(original, index + 1, current, substitutions, results);

            // Path 2: Use alternatives from confusion matrix
            if (ConfusionMatrix.TryGetValue(char.ToUpperInvariant(original[index]), out var alternatives))
            {
                foreach (var alt in alternatives)
                {
                    current[index] = alt;
                    GenerateCombinations(original, index + 1, current, substitutions + 1, results);
                }
            }
        }
    }
}
