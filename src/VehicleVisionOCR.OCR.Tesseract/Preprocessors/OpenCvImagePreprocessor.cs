using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using VehicleVisionOCR.OCR.Core.Interfaces;
using VehicleVisionOCR.OCR.Core.Models;

namespace VehicleVisionOCR.OCR.Tesseract.Preprocessors
{
    public abstract class OpenCvImagePreprocessor : IImagePreprocessor
    {
        protected readonly ILogger _logger;

        protected OpenCvImagePreprocessor(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<byte[]> ProcessAsync(byte[] imageBytes, ProcessingProfile profile)
        {
            if (!ShouldProcess(profile))
            {
                return imageBytes; // Skip processing if disabled in profile
            }

            return await Task.Run(() =>
            {
                try
                {
                    using var srcMat = Cv2.ImDecode(imageBytes, ImreadModes.Unchanged);
                    
                    if (srcMat.Empty())
                        throw new ArgumentException("Failed to decode image data.");

                    using var dstMat = new Mat();
                    ProcessMat(srcMat, dstMat, profile);

                    return dstMat.ImEncode(".png");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to process image in {this.GetType().Name}");
                    return imageBytes; // Return original on failure to not break pipeline
                }
            });
        }

        protected abstract bool ShouldProcess(ProcessingProfile profile);
        
        protected abstract void ProcessMat(Mat src, Mat dst, ProcessingProfile profile);
    }
}
