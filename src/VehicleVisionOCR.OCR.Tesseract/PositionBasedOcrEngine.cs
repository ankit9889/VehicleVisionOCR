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
                    int topHeight = (int)(height * 0.35); // Top 35% for VIN
                    int bottomHeight = (int)(height * 0.40); // Bottom 40%
                    int bottomY = height - bottomHeight;
                    int halfWidth = width / 2;

                    using var topMat = new Mat(srcMat, new OpenCvSharp.Rect(0, 0, width, topHeight));
                    using var bottomLeftMat = new Mat(srcMat, new OpenCvSharp.Rect(0, bottomY, halfWidth, bottomHeight));
                    using var bottomRightMat = new Mat(srcMat, new OpenCvSharp.Rect(halfWidth, bottomY, width - halfWidth, bottomHeight));

                    Cv2.ImEncode(".jpg", topMat, out byte[] topBytes);
                    
                    // 2. Process TOP part for VIN ONLY
                    // We can safely use _baseEngine because its regex consensus is highly optimized for VINs
                    var topResult = _baseEngine.ProcessImageAsync(topBytes).GetAwaiter().GetResult();
                    var vinField = topResult.ExtractedFields.FirstOrDefault(f => f.Key == "VIN");
                    if (vinField != null)
                    {
                        result.ExtractedFields.Add(vinField);
                    }
                    
                    result.OverallConfidence = topResult.OverallConfidence;
                    result.RawText = "--- TOP (VIN) ---\n" + topResult.RawText + "\n";

                    // 3. Process BOTTOM LEFT for Model EXACTLY
                    string modelText = RunSimpleOcr(bottomLeftMat);
                    // The Model text might have multiple lines (e.g. CB190XS ID \n PB417). 
                    // Let's just take the first line as Model
                    string model = modelText.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? "";
                    
                    if (!string.IsNullOrEmpty(model))
                    {
                        result.ExtractedFields.Add(new OcrField { Key = "Model", Value = model, Confidence = new OcrConfidence { Percentage = 90 } });
                    }

                    // 4. Process BOTTOM RIGHT for Color EXACTLY
                    string colorText = RunSimpleOcr(bottomRightMat);
                    // The Color text might have multiple lines (e.g. K1LJ D10 ID \n RACIING GREEN).
                    // Usually color is the last line or we take the whole thing
                    var colorLines = colorText.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToList();
                    string color = colorLines.LastOrDefault(l => l.Length > 3) ?? colorLines.LastOrDefault() ?? "";
                    
                    // Clean up any stray single characters (like the 'G' from noise)
                    color = Regex.Replace(color, @"\s+[A-Z]\s*$", "");
                    
                    if (!string.IsNullOrEmpty(color))
                    {
                        result.ExtractedFields.Add(new OcrField { Key = "Color", Value = color, Confidence = new OcrConfidence { Percentage = 90 } });
                    }

                    result.RawText += "\n--- BOTTOM LEFT (MODEL) ---\n" + modelText;
                    result.RawText += "\n--- BOTTOM RIGHT (COLOR) ---\n" + colorText;
                    
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Position Based OCR processing failed.");
                    throw;
                }
            });
        }

        private string RunSimpleOcr(Mat mat)
        {
            try
            {
                using var engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.LstmOnly);
                engine.DefaultPageSegMode = PageSegMode.SingleBlock;
                
                string bestText = "";
                float bestConfidence = -1f;

                // Pass 1: Grayscale + Resize + Otsu
                using (var pass1 = new Mat())
                {
                    using var gray = new Mat();
                    Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);
                    using var enlarged = new Mat();
                    Cv2.Resize(gray, enlarged, new OpenCvSharp.Size(gray.Width * 2, gray.Height * 2), 0, 0, InterpolationFlags.Cubic);
                    Cv2.Threshold(enlarged, pass1, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                    
                    var (text, conf) = ExtractTextAndConfidence(engine, pass1);
                    if (conf > bestConfidence) { bestConfidence = conf; bestText = text; }
                }

                // Pass 2: Grayscale + Resize ONLY (No Thresholding, let Tesseract handle it)
                using (var pass2 = new Mat())
                {
                    Cv2.CvtColor(mat, pass2, ColorConversionCodes.BGR2GRAY);
                    Cv2.Resize(pass2, pass2, new OpenCvSharp.Size(pass2.Width * 2, pass2.Height * 2), 0, 0, InterpolationFlags.Cubic);
                    
                    var (text, conf) = ExtractTextAndConfidence(engine, pass2);
                    if (conf > bestConfidence) { bestConfidence = conf; bestText = text; }
                }

                // Pass 3: Blur + Otsu (Helps with noisy background)
                using (var pass3 = new Mat())
                {
                    using var gray = new Mat();
                    Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);
                    using var enlarged = new Mat();
                    Cv2.Resize(gray, enlarged, new OpenCvSharp.Size(gray.Width * 2, gray.Height * 2), 0, 0, InterpolationFlags.Cubic);
                    Cv2.GaussianBlur(enlarged, enlarged, new OpenCvSharp.Size(3, 3), 0);
                    Cv2.Threshold(enlarged, pass3, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                    
                    var (text, conf) = ExtractTextAndConfidence(engine, pass3);
                    if (conf > bestConfidence) { bestConfidence = conf; bestText = text; }
                }

                return bestText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Simple OCR pass failed.");
                return "";
            }
        }

        private (string text, float confidence) ExtractTextAndConfidence(TesseractEngine engine, Mat binaryImage)
        {
            try
            {
                Cv2.ImEncode(".png", binaryImage, out byte[] imgBytes);
                using var img = Pix.LoadFromMemory(imgBytes);
                using var page = engine.Process(img);
                return (page.GetText()?.Trim() ?? "", page.GetMeanConfidence());
            }
            catch
            {
                return ("", 0f);
            }
        }
    }
}
