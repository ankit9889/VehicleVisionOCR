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

        /// <inheritdoc/>
        public async Task<List<CandidateScore>> GenerateCandidatesAsync(string normalizedBase)
        {
            var candidates = new List<CandidateScore>
            {
                // Always include the baseline candidate
                new CandidateScore { Candidate = normalizedBase, Rules = new List<string>() }
            };

            if (normalizedBase.Length < 3) return candidates;

            string prefix3 = normalizedBase.Substring(0, 3);
            
            if (!_cache.TryGetValue("WmiPrefixes", out HashSet<string> knownWmis))
            {
                await _wmiSemaphore.WaitAsync();
                try
                {
                    if (!_cache.TryGetValue("WmiPrefixes", out knownWmis))
                    {
                        var list = await _wmiRepository.GetActiveWmiPrefixesAsync();
                        knownWmis = list.ToHashSet();
                        _cache.Set("WmiPrefixes", knownWmis, System.TimeSpan.FromMinutes(_options.CacheExpirationMinutes));
                    }
                }
                finally
                {
                    _wmiSemaphore.Release();
                }
            }

            if (knownWmis == null || knownWmis.Count == 0) return candidates;

            if (!knownWmis.Contains(prefix3))
            {
                foreach (var wmi in knownWmis)
                {
                    int distance = FuzzyMatcher.ComputeLevenshteinDistance(prefix3, wmi);
                    if (distance == 1) // Fuzzy match 1 character off
                    {
                        var fuzzyCandidate = wmi + normalizedBase.Substring(3);
                        candidates.Add(new CandidateScore
                        {
                            Candidate = fuzzyCandidate,
                            Rules = new List<string> { $"Fuzzy WMI Match ({prefix3} -> {wmi})" }
                        });
                    }
                }
            }

            return candidates;
        }
    }
}
