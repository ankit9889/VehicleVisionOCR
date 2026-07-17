using System;
using System.Collections.Generic;
using VehicleVisionOCR.Domain.Vision.Enums;

namespace VehicleVisionOCR.Domain.Vision.Models
{
    public class FusionResult
    {
        public ZoneType Region { get; set; }
        
        public string WinningText { get; set; }
        public double Confidence { get; set; }
        
        public List<FusedStringCandidate> AlternativeCandidates { get; set; } = new List<FusedStringCandidate>();
        
        public string WinningOcrConfiguration { get; set; }
        public string WinningPreprocessing { get; set; }
        
        public TimeSpan TotalExecutionTime { get; set; }
        
        /// <summary>
        /// Metadata showing exactly why characters were chosen over others.
        /// </summary>
        public List<CharacterCluster> CharacterEvidenceClusters { get; set; } = new List<CharacterCluster>();
    }
}
