using System.Collections.Generic;
using VehicleVisionOCR.Domain.Vision.ArtifactRecovery;

namespace VehicleVisionOCR.Application.Vision.ArtifactRecovery.Interfaces
{
    public interface IOcrArtifactRecoveryEngine
    {
        RecoveryResult ProcessSequence(IEnumerable<CharacterNode> rawSequence, OcrRecoveryOptions options);
    }
}
