namespace VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces
{
    /// <summary>
    /// Handles deterministic, structural character normalizations for Vehicle Identification Numbers (VINs).
    /// </summary>
    public interface IVinNormalizer
    {
        /// <summary>
        /// Applies universal character normalizations (e.g., removing spaces, converting 'I', 'O', 'Q' to numbers).
        /// </summary>
        /// <param name="rawText">The raw OCR string.</param>
        /// <returns>A normalized string and the list of rules that were applied.</returns>
        (string Normalized, System.Collections.Generic.List<string> AppliedRules) NormalizeUniversalRules(string rawText);

        /// <summary>
        /// Normalizes characters based on their positional constraints within the 17-character VIN structure
        /// (e.g., ensuring characters 12-17 are numeric).
        /// </summary>
        /// <param name="candidate">The 17-character VIN string.</param>
        /// <returns>The positionally normalized candidate string and the list of structural rules applied.</returns>
        (string Normalized, System.Collections.Generic.List<string> AppliedRules) NormalizeStructuralRules(string candidate);
    }
}
