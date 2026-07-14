using System;
using System.Collections.Generic;
using VehicleVisionOCR.OCR.Core.Enums;

namespace VehicleVisionOCR.OCR.Core.Models
{
    public class ImageMetadata
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Format { get; set; } = string.Empty;
        public double ResolutionDpi { get; set; }
    }

    public class ProcessingProfile
    {
        public bool EnableDeskew { get; set; } = true;
        public bool EnableNoiseReduction { get; set; } = true;
        public bool EnableThresholding { get; set; } = true;
        public bool EnableSharpening { get; set; } = true;
        public bool EnableContrastEnhancement { get; set; } = true;
    }

    public class OcrRequest
    {
        public Guid RequestId { get; set; } = Guid.NewGuid();
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
        public ProcessingProfile Profile { get; set; } = new();
        public string ExpectedFormat { get; set; } = string.Empty; // e.g. "VIN", "LicensePlate"
    }

    public class OcrConfidence
    {
        public double Percentage { get; set; }
        public bool IsReliable => Percentage >= 75.0; // Configurable threshold in real app
    }

    public class DetectedText
    {
        public string Text { get; set; } = string.Empty;
        public OcrConfidence Confidence { get; set; } = new();
        // Simplified bounding box (X, Y, Width, Height)
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class OcrField
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public OcrConfidence Confidence { get; set; } = new();
    }

    public class OcrResultData
    {
        public string RawText { get; set; } = string.Empty;
        public List<DetectedText> DetectedTexts { get; set; } = new();
        public List<OcrField> ExtractedFields { get; set; } = new();
        public OcrConfidence OverallConfidence { get; set; } = new();
    }

    public class OcrResponse
    {
        public Guid RequestId { get; set; }
        public OcrStatus Status { get; set; }
        public OcrResultData Result { get; set; } = new();
        public TimeSpan ProcessingTime { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
