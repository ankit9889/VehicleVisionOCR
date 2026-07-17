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

        // Structural and Optional Evidence
        public int LineIndex { get; set; }
        public int WordIndex { get; set; }
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public bool IsMonospace { get; set; }
        public System.Collections.Generic.List<CharacterChoice> Alternatives { get; set; } = new System.Collections.Generic.List<CharacterChoice>();
    }

    public class CharacterChoice
    {
        public char Character { get; set; }
        public double Confidence { get; set; }
    }
}
