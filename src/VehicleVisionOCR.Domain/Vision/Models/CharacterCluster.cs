using System.Collections.Generic;

namespace VehicleVisionOCR.Domain.Vision.Models
{
    public class CharacterCluster
    {
        public int PositionIndex { get; set; }
        
        /// <summary>
        /// All character evidence observed at this geometric location across multiple OCR passes.
        /// </summary>
        public List<CharacterEvidence> EvidenceList { get; set; } = new List<CharacterEvidence>();
        
        /// <summary>
        /// The bounding box spanning the clustered evidence.
        /// </summary>
        public int BoundingBoxX { get; set; }
        public int BoundingBoxY { get; set; }
        public int BoundingBoxWidth { get; set; }
        public int BoundingBoxHeight { get; set; }
    }
}
