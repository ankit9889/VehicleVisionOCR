using System.Collections.Generic;

namespace VehicleVisionOCR.Domain.Vision.ArtifactRecovery
{
    public class CharacterNode
    {
        public char PrimaryChar { get; set; }
        public double Confidence { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public List<VehicleVisionOCR.Domain.Vision.Models.CharacterChoice> Alternatives { get; set; } = new List<VehicleVisionOCR.Domain.Vision.Models.CharacterChoice>();
        
        // Contextual reference for application layer
        public object SourceReference { get; set; }
    }

    public class RecoveryResult
    {
        public string OriginalText { get; set; }
        public string CorrectedText { get; set; }
        public double OriginalConfidence { get; set; }
        public double CorrectedConfidence { get; set; }
        public List<RepairRecord> AppliedRepairs { get; set; } = new List<RepairRecord>();
        public List<CharacterNode> RecoveredSequence { get; set; } = new List<CharacterNode>();
    }

    public class RepairRecord
    {
        public ArtifactRepairType RepairType { get; set; }
        public string TargetText { get; set; }
        public string ReplacedWith { get; set; }
        public double ConfidencePenalty { get; set; }
        public string MathematicalReason { get; set; }
    }

    public enum ArtifactRepairType
    {
        NoiseRemoval,
        DuplicateSuppression,
        InsertionOutlierRemoval,
        SplitCharacterMerge,
        GeometricSubstitution
    }

    public class OcrRecoveryOptions
    {
        public double MaxBaselineDeviationPercentage { get; set; } = 0.20;
        public double MinWidthOverlapForMerge { get; set; } = 0.60;
        public double NoiseConfidenceThreshold { get; set; } = 60.0;
        public double ConfidencePenaltyPerMerge { get; set; } = 5.0;
        public double ConfidencePenaltyPerRemoval { get; set; } = 10.0;
    }

    public class RepairAction
    {
        public ArtifactRepairType Type { get; set; }
        public double ConfidencePenalty { get; set; }
        public string Reason { get; set; }
        public List<CharacterNode> ModifiedNodes { get; set; } = new List<CharacterNode>();
    }

    public class RecoveryContext
    {
        public LinkedList<CharacterNode> Sequence { get; set; }
        public OcrRecoveryOptions Config { get; set; }
        public double MedianWidth { get; set; }
        public double MedianHeight { get; set; }
        public double MedianY { get; set; }
    }
}
