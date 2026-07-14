using System.Threading.Tasks;

namespace VehicleVisionOCR.Application.Interfaces
{
    /// <summary>
    /// Pre-processes images before OCR to improve accuracy (e.g., OpenCV implementation).
    /// </summary>
    public interface IImageProcessor
    {
        /// <summary>
        /// Applies noise reduction, binarization, and deskewing to an image.
        /// </summary>
        /// <returns>The path to the processed image file.</returns>
        Task<string> ProcessImageAsync(string sourceImagePath);
    }
}
