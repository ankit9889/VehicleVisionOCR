using System.Collections.Generic;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Domain.Vision.Interfaces
{
    public interface IOcrCandidateCollector
    {
        List<CharacterCluster> ClusterObservations(List<OcrObservation> rawPasses);
    }
}
