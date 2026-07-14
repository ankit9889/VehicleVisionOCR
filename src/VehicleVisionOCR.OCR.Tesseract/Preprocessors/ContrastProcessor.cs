using Microsoft.Extensions.Logging;
using OpenCvSharp;
using VehicleVisionOCR.OCR.Core.Interfaces;
using VehicleVisionOCR.OCR.Core.Models;

namespace VehicleVisionOCR.OCR.Tesseract.Preprocessors
{
    public class ContrastProcessor : OpenCvImagePreprocessor, IImageEnhancer
    {
        public ContrastProcessor(ILogger<ContrastProcessor> logger) : base(logger) { }

        protected override bool ShouldProcess(ProcessingProfile profile) => profile.EnableContrastEnhancement;

        protected override void ProcessMat(Mat src, Mat dst, ProcessingProfile profile)
        {
            _logger.LogInformation("Applying Contrast Enhancement (CLAHE).");

            using var gray = new Mat();
            if (src.Channels() > 1)
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
            else
                src.CopyTo(gray);

            // CLAHE (Contrast Limited Adaptive Histogram Equalization)
            using var clahe = Cv2.CreateCLAHE(clipLimit: 2.0, tileGridSize: new Size(8, 8));
            clahe.Apply(gray, dst);
        }
    }
}
