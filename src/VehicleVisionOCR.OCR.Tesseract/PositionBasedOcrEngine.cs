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
                    // Adjusted to completely avoid the barcode (Barcode is usually 30% to 70%)
                    int topHeight = (int)(height * 0.30); // Top 30% for VIN
                    int bottomHeight = (int)(height * 0.25); // Bottom 25% for Model/Color
                    int bottomY = height - bottomHeight;
                    int halfWidth = width / 2;

                    using var topMat = new Mat(srcMat, new OpenCvSharp.Rect(0, 0, width, topHeight));
                    using var bottomLeftMat = new Mat(srcMat, new OpenCvSharp.Rect(0, bottomY, halfWidth, bottomHeight));
                    using var bottomRightMat = new Mat(srcMat, new OpenCvSharp.Rect(halfWidth, bottomY, width - halfWidth, bottomHeight));

                    // Use PNG to prevent JPEG compression artifacts from ruining text edges
                    Cv2.ImEncode(".png", topMat, out byte[] topBytes);
                    
                    // 2. Process TOP part for VIN ONLY
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
                    string model = modelText.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? "";
                    
                    if (!string.IsNullOrEmpty(model))
                    {
                        result.ExtractedFields.Add(new OcrField { Key = "Model", Value = model, Confidence = new OcrConfidence { Percentage = 90 } });
                    }

                    // 4. Process BOTTOM RIGHT for Color EXACTLY
                    string colorText = RunSimpleOcr(bottomRightMat);
                    var colorLines = colorText.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToList();
                    string color = colorLines.LastOrDefault(l => l.Length > 3) ?? colorLines.LastOrDefault() ?? "";
                    
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
                // For exact cropped regions, treat it as a single block of text
                engine.DefaultPageSegMode = PageSegMode.SingleBlock;
                
                string bestText = "";
                float bestConfidence = -1f;

                // Pass 1: MinRGB (Perfect for ANY colored text on white background)
                using (var pass1 = new Mat())
                {
                    var channels = Cv2.Split(mat);
                    if (channels.Length == 3)
                    {
                        using var minBG = new Mat();
                        Cv2.Min(channels[0], channels[1], minBG);
                        Cv2.Min(minBG, channels[2], pass1); // pass1 is now Min(B, G, R)
                        
                        using var enlarged = new Mat();
                        Cv2.Resize(pass1, enlarged, new OpenCvSharp.Size(pass1.Width * 2, pass1.Height * 2), 0, 0, InterpolationFlags.Cubic);
                        
                        using var binary = new Mat();
                        Cv2.Threshold(enlarged, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                        
                        var (text, conf) = ExtractTextAndConfidence(engine, binary);
                        if (conf > bestConfidence) { bestConfidence = conf; bestText = text; }
                    }
                }

                // Pass 2: MinRGB + Morphological Barcode Removal (In case crop still catches barcode edges)
                using (var pass2 = new Mat())
                {
                    var channels = Cv2.Split(mat);
                    if (channels.Length == 3)
                    {
                        using var minBG = new Mat();
                        Cv2.Min(channels[0], channels[1], minBG);
                        Cv2.Min(minBG, channels[2], pass2); 
                        
                        using var enlarged = new Mat();
                        Cv2.Resize(pass2, enlarged, new OpenCvSharp.Size(pass2.Width * 2, pass2.Height * 2), 0, 0, InterpolationFlags.Cubic);
                        
                        using var binary = new Mat();
                        Cv2.Threshold(enlarged, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                        
                        // To remove black vertical lines on white background, we use Close (Dilate then Erode)
                        var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(15, 1));
                        Cv2.MorphologyEx(binary, binary, MorphTypes.Close, kernel);
                        
                        var (text, conf) = ExtractTextAndConfidence(engine, binary);
                        if (conf > bestConfidence) { bestConfidence = conf; bestText = text; }
                    }
                }

                // Pass 3: Standard Grayscale + Blur + Otsu (Fallback)
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
