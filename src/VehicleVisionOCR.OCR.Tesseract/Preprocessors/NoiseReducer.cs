using Microsoft.Extensions.Logging;
using OpenCvSharp;
using VehicleVisionOCR.OCR.Core.Interfaces;
using VehicleVisionOCR.OCR.Core.Models;

namespace VehicleVisionOCR.OCR.Tesseract.Preprocessors
{
    public class NoiseReducer : OpenCvImagePreprocessor, IImageNoiseReducer
    {
        public NoiseReducer(ILogger<NoiseReducer> logger) : base(logger) { }

        protected override bool ShouldProcess(ProcessingProfile profile) => profile.EnableNoiseReduction;

        protected override void ProcessMat(Mat src, Mat dst, ProcessingProfile profile)
        {
            _logger.LogInformation("Applying Noise Reduction.");
            
            // FastNlMeansDenoising works best for removing grain while preserving edges
            if (src.Channels() == 3)
            {
                Cv2.FastNlMeansDenoisingColored(src, dst, 10, 10, 7, 21);
            }
            else
            {
                Cv2.FastNlMeansDenoising(src, dst, 10, 7, 21);
            }
        }
    }
}
