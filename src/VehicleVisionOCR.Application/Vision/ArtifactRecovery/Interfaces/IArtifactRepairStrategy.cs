using System.Collections.Generic;
using VehicleVisionOCR.Domain.Vision.ArtifactRecovery;

namespace VehicleVisionOCR.Application.Vision.ArtifactRecovery.Interfaces
{
    public interface IArtifactRepairStrategy
    {
        int ExecutionOrder { get; }
        bool TryRepair(LinkedListNode<CharacterNode> current, RecoveryContext context, out RepairAction action);
    }
}
