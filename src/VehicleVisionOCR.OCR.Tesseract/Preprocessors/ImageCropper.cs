using Microsoft.Extensions.Logging;
using OpenCvSharp;
using VehicleVisionOCR.OCR.Core.Interfaces;
using VehicleVisionOCR.OCR.Core.Models;

namespace VehicleVisionOCR.OCR.Tesseract.Preprocessors
{
    public class ImageCropper : OpenCvImagePreprocessor, IImageCropper
    {
        public ImageCropper(ILogger<ImageCropper> logger) : base(logger) { }

        protected override bool ShouldProcess(ProcessingProfile profile) => true; // Could have a specific flag in profile

        protected override void ProcessMat(Mat src, Mat dst, ProcessingProfile profile)
        {
            _logger.LogInformation("Applying Image Crop (Removing boundaries).");
            
            // Simple edge crop heuristic (crop 5% from all sides) to remove black borders from scanning
            int cropX = (int)(src.Width * 0.05);
            int cropY = (int)(src.Height * 0.05);
            int cropWidth = src.Width - (cropX * 2);
            int cropHeight = src.Height - (cropY * 2);

            var roi = new Rect(cropX, cropY, cropWidth, cropHeight);
            using var cropped = new Mat(src, roi);
            cropped.CopyTo(dst);
        }
    }
}
