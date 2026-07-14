using Microsoft.Extensions.Logging;
using OpenCvSharp;
using VehicleVisionOCR.OCR.Core.Interfaces;
using VehicleVisionOCR.OCR.Core.Models;

namespace VehicleVisionOCR.OCR.Tesseract.Preprocessors
{
    public class ImageRotation : OpenCvImagePreprocessor, IImagePreprocessor
    {
        public ImageRotation(ILogger<ImageRotation> logger) : base(logger) { }

        protected override bool ShouldProcess(ProcessingProfile profile) => true; 

        protected override void ProcessMat(Mat src, Mat dst, ProcessingProfile profile)
        {
            _logger.LogInformation("Applying Auto Rotation.");
            
            // In a real application, you might use OpenCV to detect text orientation
            // and apply 90, 180, 270 degree rotation. 
            // For now, we just copy it over as placeholder for the strategy pattern.
            src.CopyTo(dst);
        }
    }
}
