namespace VehicleVisionOCR.Domain.Vision.Models
{
    public class RegionFeatures
    {
        public double Area { get; set; }
        public double AspectRatio { get; set; }
        public double FillDensity { get; set; } // Area of contour / Bounding Box Area
        public bool IsBarcode { get; set; } // Derived from parallel line density / Sobel X
        public int DistanceToBarcode { get; set; } // Euclidean distance to the detected barcode anchor
        public double RelativeVerticalPosition { get; set; } // 0.0 (Top) to 1.0 (Bottom)
        public double RelativeHorizontalPosition { get; set; } // 0.0 (Left) to 1.0 (Right)
        
        // Approximations based on OpenCV bounding boxes
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
