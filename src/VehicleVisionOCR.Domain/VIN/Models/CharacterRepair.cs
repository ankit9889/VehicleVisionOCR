namespace VehicleVisionOCR.Domain.VIN.Models
{
    public class CharacterRepair
    {
        public int PositionIndex { get; set; }
        public char OriginalCharacter { get; set; }
        public char RepairedCharacter { get; set; }
        public string Justification { get; set; }
    }
}
