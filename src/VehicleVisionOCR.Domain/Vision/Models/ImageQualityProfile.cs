namespace VehicleVisionOCR.Domain.Vision.Models
{
    public class ImageQualityProfile
    {
        /// <summary>
        /// Variance of the Laplacian. Lower values mean blurrier images.
        /// </summary>
        public double BlurScore { get; set; }
        
        /// <summary>
        /// Is the BlurScore below the acceptable threshold?
        /// </summary>
        public bool IsBlurry { get; set; }
        
        /// <summary>
        /// True if saturated pixel clusters intersect with text zones.
        /// </summary>
        public bool GlareDetected { get; set; }
        
        /// <summary>
        /// Measure of image contrast (Standard deviation of pixel intensities).
        /// </summary>
        public double ContrastScore { get; set; }
        
        /// <summary>
        /// True if contrast is too low to extract text without illumination normalization.
        /// </summary>
        public bool LowContrast { get; set; }
    }
}
