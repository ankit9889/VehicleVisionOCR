using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using VehicleVisionOCR.OCR.Core.Interfaces;
using VehicleVisionOCR.OCR.Core.Models;
using Tesseract;
using System.IO;

namespace VehicleVisionOCR.OCR.Tesseract
{
    public class PositionBasedOcrEngine : IOcrEngine
    {
        private readonly ILogger _logger;
        private readonly string _tessDataPath;
        private readonly TesseractOcrEngine _baseEngine;

        public PositionBasedOcrEngine(ILogger logger, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _logger = logger;
            
            // Re-use Tesseract Engine for underlying processing.
            // We can pass a null logger or cast if needed, but since TesseractOcrEngine expects ILogger<TesseractOcrEngine>, 
            // we will just use a NullLogger to satisfy its dependency since we log in this class anyway.
            _baseEngine = new TesseractOcrEngine(Microsoft.Extensions.Logging.Abstractions.NullLogger<TesseractOcrEngine>.Instance, configuration);

            var customPath = configuration?["Tesseract:DataPath"];
            if (!string.IsNullOrEmpty(customPath) && Directory.Exists(customPath))
            {
                _tessDataPath = customPath;
            }
            else
            {
                var exeDir = AppContext.BaseDirectory;
                _tessDataPath = Path.Combine(exeDir, "tessdata");
            }
        }

        public string EngineName => "TesseractOCR.PositionBased.V1";
        public string Version => "1.0.0";
        public bool IsReady => Directory.Exists(_tessDataPath);

        public async Task<OcrResultData> ProcessImageAsync(byte[] imageData)
        {
            return await Task.Run(() =>
            {
                var result = new OcrResultData
                {
                    ExtractedFields = new List<OcrField>()
                };

                try
                {
                    using var srcMat = Cv2.ImDecode(imageData, ImreadModes.Color);
                    if (srcMat.Empty())
                    {
                        throw new ArgumentException("Failed to decode image data into OpenCV Mat.");
                    }

                    int height = srcMat.Rows;
                    int width = srcMat.Cols;

                    // 1. Split Image based on Position
                    // TOP 35% -> VIN (Above barcode)
                    // BOTTOM 40% -> Color & Model (Below barcode)
                    
                    int topHeight = (int)(height * 0.35);
                    int bottomHeight = (int)(height * 0.40);
                    int bottomY = height - bottomHeight;

                    using var topMat = new Mat(srcMat, new OpenCvSharp.Rect(0, 0, width, topHeight));
                    using var bottomMat = new Mat(srcMat, new OpenCvSharp.Rect(0, bottomY, width, bottomHeight));

                    // Encode back to bytes to pass to base engine
                    Cv2.ImEncode(".jpg", topMat, out byte[] topBytes);
                    Cv2.ImEncode(".jpg", bottomMat, out byte[] bottomBytes);

                    // 2. Process TOP part for VIN ONLY
                    var topResult = _baseEngine.ProcessImageAsync(topBytes).GetAwaiter().GetResult();
                    var vinField = topResult.ExtractedFields.FirstOrDefault(f => f.Key == "VIN");
                    if (vinField != null)
                    {
                        result.ExtractedFields.Add(vinField);
                    }
                    
                    result.OverallConfidence = topResult.OverallConfidence;
                    result.RawText = "--- TOP SECTION ---\n" + topResult.RawText + "\n";

                    // 3. Process BOTTOM part for Color and Model ONLY
                    var bottomResult = _baseEngine.ProcessImageAsync(bottomBytes).GetAwaiter().GetResult();
                    var colorField = bottomResult.ExtractedFields.FirstOrDefault(f => f.Key == "Color");
                    var modelField = bottomResult.ExtractedFields.FirstOrDefault(f => f.Key == "Model");

                    if (colorField != null) result.ExtractedFields.Add(colorField);
                    if (modelField != null) result.ExtractedFields.Add(modelField);

                    result.RawText += "\n--- BOTTOM SECTION ---\n" + bottomResult.RawText;
                    
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Position Based OCR processing failed.");
                    throw;
                }
            });
        }
    }
}
