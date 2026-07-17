using System.Collections.Generic;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Domain.Vision.Interfaces
{
    public interface ICharacterVotingEngine
    {
        FusedStringCandidate VoteOnClusters(List<CharacterCluster> clusters);
    }
}
