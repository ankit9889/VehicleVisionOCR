using System.Collections.Generic;
using System.Threading.Tasks;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Models;

namespace VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces
{
    /// <summary>
    /// Generates potential variations (candidates) for a scanned VIN, primarily by fuzzy-matching
    /// the World Manufacturer Identifier (WMI) against known database prefixes.
    /// </summary>
    public interface IVinCandidateGenerator
    {
        /// <summary>
        /// Generates a list of valid candidates based on the normalized base string.
        /// </summary>
        /// <param name="normalizedBase">The initial normalized 17-character VIN string.</param>
        /// <returns>A collection of candidate scores outlining the variations and rules applied to generate them.</returns>
        Task<List<CandidateScore>> GenerateCandidatesAsync(string normalizedBase);
    }
}
