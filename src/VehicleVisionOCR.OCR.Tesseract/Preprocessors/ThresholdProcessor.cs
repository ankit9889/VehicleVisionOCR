using Microsoft.Extensions.Logging;
using OpenCvSharp;
using VehicleVisionOCR.OCR.Core.Interfaces;
using VehicleVisionOCR.OCR.Core.Models;

namespace VehicleVisionOCR.OCR.Tesseract.Preprocessors
{
    public class ThresholdProcessor : OpenCvImagePreprocessor, IImageThreshold
    {
        public ThresholdProcessor(ILogger<ThresholdProcessor> logger) : base(logger) { }

        protected override bool ShouldProcess(ProcessingProfile profile) => profile.EnableThresholding;

        protected override void ProcessMat(Mat src, Mat dst, ProcessingProfile profile)
        {
            _logger.LogInformation("Applying Adaptive Thresholding.");

            using var gray = new Mat();
            if (src.Channels() > 1)
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
            else
                src.CopyTo(gray);

            // Adaptive thresholding handles varying lighting conditions well
            Cv2.AdaptiveThreshold(
                gray, 
                dst, 
                255, 
                AdaptiveThresholdTypes.GaussianC, 
                ThresholdTypes.Binary, 
                11, 
                2);
        }
    }
}
