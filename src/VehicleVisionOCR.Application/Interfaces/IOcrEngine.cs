using System.Threading.Tasks;
using VehicleVisionOCR.Domain.Entities;

namespace VehicleVisionOCR.Application.Interfaces
{
    /// <summary>
    /// Interface for OCR engines (e.g., Tesseract, Azure).
    /// </summary>
    public interface IOcrEngine
    {
        string EngineName { get; }
        
        /// <summary>
        /// Extracts structured text from a given image path.
        /// </summary>
        Task<OCRResult> ExtractTextAsync(string imagePath);
    }
}
