namespace VehicleVisionOCR.Domain.Vision.Models
{
    public class CroppedZone
    {
        public Enums.ZoneType Type { get; set; }
        
        /// <summary>
        /// X coordinate of the top-left corner in the original image.
        /// </summary>
        public int X { get; set; }
        
        /// <summary>
        /// Y coordinate of the top-left corner in the original image.
        /// </summary>
        public int Y { get; set; }
        
        public int Width { get; set; }
        public int Height { get; set; }
        
        /// <summary>
        /// The raw bytes of the cropped image region.
        /// </summary>
        public byte[] ImageData { get; set; }
        
        /// <summary>
        /// Confidence that this zone represents what the Type says it represents (0.0 to 1.0).
        /// </summary>
        public double ZoneConfidence { get; set; }
        
        public CroppedZone(Enums.ZoneType type, int x, int y, int width, int height, byte[] imageData, double zoneConfidence = 1.0)
        {
            Type = type;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            ImageData = imageData;
            ZoneConfidence = zoneConfidence;
        }
    }
}
