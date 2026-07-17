using VehicleVisionOCR.Domain.VIN.Models;

namespace VehicleVisionOCR.Domain.VIN.Interfaces
{
    public interface IVinRule
    {
        string Name { get; }
        
        /// <summary>
        /// Evaluates the candidate and adds points, bonuses, or rule violations to the candidate's score.
        /// </summary>
        void Evaluate(VinCandidate candidate, VinReasoningConfig config);
    }
}
