using System;

namespace VehicleVisionOCR.OCR.Tesseract.Exceptions
{
    public class TesseractOcrException : Exception
    {
        public TesseractOcrException(string message) : base(message)
        {
        }

        public TesseractOcrException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
