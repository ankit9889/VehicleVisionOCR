using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Domain.Vision.Interfaces
{
    public interface IImageQualityAnalyzer
    {
        /// <summary>
        /// Analyzes a raw image byte array and generates a quality profile (blur, glare, contrast).
        /// </summary>
        ImageQualityProfile Analyze(byte[] rawImageData);
    }
}
