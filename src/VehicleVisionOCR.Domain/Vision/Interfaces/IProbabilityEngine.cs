using System.Collections.Generic;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Domain.Vision.Interfaces
{
    public interface IProbabilityEngine
    {
        double CalculateCharacterProbability(CharacterEvidence evidence, CharacterCluster parentCluster, int totalPasses);
    }
}
