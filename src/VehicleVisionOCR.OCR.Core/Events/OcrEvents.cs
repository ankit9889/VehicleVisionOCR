using System;
using VehicleVisionOCR.OCR.Core.Models;

namespace VehicleVisionOCR.OCR.Core.Events
{
    public class ImageLoadedEventArgs : EventArgs 
    { 
        public Guid RequestId { get; set; } 
    }

    public class PreprocessingStartedEventArgs : EventArgs 
    { 
        public Guid RequestId { get; set; } 
    }

    public class PreprocessingCompletedEventArgs : EventArgs 
    { 
        public Guid RequestId { get; set; }
        public byte[] ProcessedImageData { get; set; } = Array.Empty<byte>(); 
    }

    public class OcrStartedEventArgs : EventArgs 
    { 
        public Guid RequestId { get; set; } 
        public string EngineName { get; set; } = string.Empty;
    }

    public class OcrCompletedEventArgs : EventArgs 
    { 
        public Guid RequestId { get; set; } 
        public OcrResponse Response { get; set; } = new();
    }

    public class ValidationCompletedEventArgs : EventArgs 
    { 
        public Guid RequestId { get; set; } 
        public bool IsValid { get; set; }
    }

    public class ProcessingFailedEventArgs : EventArgs 
    { 
        public Guid RequestId { get; set; } 
        public string ErrorMessage { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
    }
}
