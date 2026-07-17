using OpenCvSharp;
using VehicleVisionOCR.Domain.Vision.Interfaces;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Infrastructure.Vision.OpenCV
{
    public class ImageQualityAnalyzer : IImageQualityAnalyzer
    {
        private const double BlurThreshold = 100.0; // Adjustable threshold

        public ImageQualityProfile Analyze(byte[] rawImageData)
        {
            var profile = new ImageQualityProfile();

            using (var mat = Cv2.ImDecode(rawImageData, ImreadModes.Grayscale))
            {
                if (mat.Empty())
                {
                    return profile;
                }

                profile.BlurScore = CalculateBlurScore(mat);
                profile.IsBlurry = profile.BlurScore < BlurThreshold;

                profile.ContrastScore = CalculateContrast(mat);
                profile.LowContrast = profile.ContrastScore < 50.0; // Basic standard deviation threshold

                profile.GlareDetected = DetectGlare(mat);
            }

            return profile;
        }

        private double CalculateBlurScore(Mat grayscaleImage)
        {
            using (var laplacian = new Mat())
            {
                Cv2.Laplacian(grayscaleImage, laplacian, MatType.CV_64F);
                Cv2.MeanStdDev(laplacian, out _, out var stddev);
                double variance = stddev.Val0 * stddev.Val0;
                return variance;
            }
        }

        private double CalculateContrast(Mat grayscaleImage)
        {
            Cv2.MeanStdDev(grayscaleImage, out _, out var stddev);
            return stddev.Val0;
        }

        private bool DetectGlare(Mat grayscaleImage)
        {
            // Glare is typically represented by large clusters of saturated (white) pixels.
            using (var threshold = new Mat())
            {
                Cv2.Threshold(grayscaleImage, threshold, 245, 255, ThresholdTypes.Binary);
                int whitePixels = Cv2.CountNonZero(threshold);
                
                // If more than 5% of the image is completely blown out white, it's likely glare
                double glarePercentage = (double)whitePixels / (grayscaleImage.Rows * grayscaleImage.Cols);
                return glarePercentage > 0.05;
            }
        }
    }
}
