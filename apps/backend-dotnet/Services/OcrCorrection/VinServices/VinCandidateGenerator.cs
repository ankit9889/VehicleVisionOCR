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

        private static readonly Dictionary<string, string[]> ConfusionMatrix = new Dictionary<string, string[]>
        {
            {"0", new[] {"O", "D", "Q"}},
            {"O", new[] {"0", "D", "Q", "C"}},
            {"1", new[] {"I", "L", "7"}},
            {"I", new[] {"1", "L", "7"}},
            {"L", new[] {"1", "I"}},
            {"5", new[] {"S", "6"}},
            {"S", new[] {"5"}},
            {"8", new[] {"B", "3"}},
            {"B", new[] {"8"}},
            {"3", new[] {"8"}},
            {"6", new[] {"G", "5"}},
            {"G", new[] {"6", "C"}},
            {"2", new[] {"Z", "7"}},
            {"Z", new[] {"2"}},
            {"7", new[] {"T", "1", "I", "2"}},
            {"T", new[] {"7", "1"}},
            {"D", new[] {"0", "O"}},
            {"Q", new[] {"0", "O"}},
            {"A", new[] {"4"}},
            {"4", new[] {"A"}},
            {"C", new[] {"G", "O"}},
            {"M", new[] {"N", "RN"}},
            {"N", new[] {"M"}},
            {"RN", new[] {"M"}},
            {"VV", new[] {"W"}},
            {"W", new[] {"VV"}}
        };

        /// <inheritdoc/>
        public async Task<List<CandidateScore>> GenerateCandidatesAsync(string normalizedBase)
        {
            var candidates = new List<CandidateScore>();
            if (string.IsNullOrEmpty(normalizedBase)) return candidates;

            GenerateCombinations(normalizedBase, 0, "", 0, candidates);
            return candidates;
        }

        private void GenerateCombinations(string original, int index, string current, int substitutions, List<CandidateScore> results)
        {
            // Limit combinatorial explosion
            if (substitutions > 4) return;
            if (results.Count >= 2000) return;

            if (index >= original.Length)
            {
                results.Add(new CandidateScore 
                { 
                    Candidate = current,
                    Rules = substitutions > 0 ? new List<string> { $"Confusion Matrix ({substitutions} changes)" } : new List<string>(),
                    Substitutions = substitutions
                });
                return;
            }

            // Path 1: Keep original character
            GenerateCombinations(original, index + 1, current + original[index], substitutions, results);

            // Path 2: Use alternatives from confusion matrix
            // Check single character
            string singleChar = original.Substring(index, 1);
            if (ConfusionMatrix.TryGetValue(singleChar.ToUpperInvariant(), out var alternatives1))
            {
                foreach (var alt in alternatives1)
                {
                    GenerateCombinations(original, index + 1, current + alt, substitutions + 1, results);
                }
            }

            // Check two-character sequence
            if (index < original.Length - 1)
            {
                string twoChars = original.Substring(index, 2);
                if (ConfusionMatrix.TryGetValue(twoChars.ToUpperInvariant(), out var alternatives2))
                {
                    foreach (var alt in alternatives2)
                    {
                        GenerateCombinations(original, index + 2, current + alt, substitutions + 1, results);
                    }
                }
            }
        }
    }
}
