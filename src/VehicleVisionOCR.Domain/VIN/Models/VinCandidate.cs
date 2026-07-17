using System.Collections.Generic;

namespace VehicleVisionOCR.Domain.VIN.Models
{
    public class VinCandidate
    {
        public string CandidateString { get; set; }
        
        public CandidateScore Score { get; set; } = new CandidateScore();
        
        public List<CharacterRepair> Repairs { get; set; } = new List<CharacterRepair>();
        
        public ManufacturerData ExtractedManufacturer { get; set; }
    }
}
