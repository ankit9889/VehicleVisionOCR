namespace VehicleVisionOCR.Domain.Vision.Models
{
    public class CharacterEvidence
    {
        public char Character { get; set; }
        public double Confidence { get; set; }
        
        // Bounding Box
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        
        public string SourcePassId { get; set; }
        public string SourcePreprocessing { get; set; }
        public int SourcePageSegmentationMode { get; set; }
        public double SourceScale { get; set; }
    }
}
