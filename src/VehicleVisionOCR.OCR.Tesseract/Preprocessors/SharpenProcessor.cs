using Microsoft.Extensions.Logging;
using OpenCvSharp;
using VehicleVisionOCR.OCR.Core.Interfaces;
using VehicleVisionOCR.OCR.Core.Models;

namespace VehicleVisionOCR.OCR.Tesseract.Preprocessors
{
    public class SharpenProcessor : OpenCvImagePreprocessor, IImageEnhancer
    {
        public SharpenProcessor(ILogger<SharpenProcessor> logger) : base(logger) { }

        protected override bool ShouldProcess(ProcessingProfile profile) => profile.EnableSharpening;

        protected override void ProcessMat(Mat src, Mat dst, ProcessingProfile profile)
        {
            _logger.LogInformation("Applying Sharpening filter.");

            // Create a sharpening kernel
            using var kernel = new Mat(3, 3, MatType.CV_32F);
            kernel.SetArray(new float[] 
            {
                0, -1, 0,
                -1, 5, -1,
                0, -1, 0
            });

            Cv2.Filter2D(src, dst, -1, kernel);
        }
    }
}
