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

                    // 1. Split Image based on Position (DYNAMIC BARCODE AVOIDANCE)
                    int barcodeTopY = (int)(height * 0.30); // Fallbacks
                    int barcodeBottomY = (int)(height * 0.70);

                    using (var minBG = new Mat())
                    using (var minRGB = new Mat())
                    using (var binary = new Mat())
                    using (var closed = new Mat())
                    using (var rowSums = new Mat())
                    {
                        var channels = Cv2.Split(srcMat);
                        if (channels.Length == 3)
                        {
                            Cv2.Min(channels[0], channels[1], minBG);
                            Cv2.Min(minBG, channels[2], minRGB);
                            
                            // Invert so text/barcode is White, background is Black
                            Cv2.Threshold(minRGB, binary, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);
                            
                            // Connect barcode lines horizontally ONLY (prevents vertical merging with VIN)
                            var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(50, 2));
                            Cv2.MorphologyEx(binary, closed, MorphTypes.Close, kernel);
                            
                            // Reduce to a single column containing the sum of white pixels for each row
                            Cv2.Reduce(closed, rowSums, ReduceDimension.Column, ReduceTypes.Sum, MatType.CV_32S);
                            
                            int thresholdScore = (int)(width * 255 * 0.3); // At least 30% of the row must be solid white
                            int maxBlockHeight = 0;
                            int currentBlockStart = -1;
                            int bestFirstY = -1;
                            int bestLastY = -1;

                            for (int y = 0; y < height; y++)
                            {
                                int sum = rowSums.At<int>(y, 0);
                                if (sum > thresholdScore)
                                {
                                    if (currentBlockStart == -1) currentBlockStart = y;
                                }
                                else
                                {
                                    if (currentBlockStart != -1)
                                    {
                                        int blockHeight = y - currentBlockStart;
                                        if (blockHeight > maxBlockHeight)
                                        {
                                            maxBlockHeight = blockHeight;
                                            bestFirstY = currentBlockStart;
                                            bestLastY = y - 1;
                                        }
                                        currentBlockStart = -1;
                                    }
                                }
                            }
                            
                            if (currentBlockStart != -1)
                            {
                                int blockHeight = height - currentBlockStart;
                                if (blockHeight > maxBlockHeight)
                                {
                                    maxBlockHeight = blockHeight;
                                    bestFirstY = currentBlockStart;
                                    bestLastY = height - 1;
                                }
                            }

                            if (bestFirstY != -1 && maxBlockHeight > height * 0.1) // Barcode must be at least 10% of image height
                            {
                                barcodeTopY = bestFirstY;
                                barcodeBottomY = bestLastY;
                                _logger.LogInformation($"Dynamically found barcode via Row Projection at Y:{barcodeTopY} to {barcodeBottomY}");
                            }
                        }
                    }

                    // Define precise horizontal crop regions
                    int topHeight = Math.Max(10, barcodeTopY - 5); // 5px padding above barcode
                    int bottomY = Math.Min(height - 10, barcodeBottomY + 5); // 5px padding below barcode
                    int bottomHeight = height - bottomY;

                    // Apply a small horizontal margin to remove border artifacts (which cause extra '0' or '1')
                    int hMargin = Math.Min(15, width / 20); 
                    int safeWidth = width - (hMargin * 2);

                    using var topMat = new Mat(srcMat, new OpenCvSharp.Rect(hMargin, 0, safeWidth, topHeight));
                    
                    // 2. Dynamic Vertical Split for Bottom Text
                    // We must find the gap between the left and right columns to avoid slicing words like "GRANITE"
                    int splitX = width / 2; // Fallback
                    using var bottomMatTemp = new Mat(srcMat, new OpenCvSharp.Rect(hMargin, bottomY, safeWidth, bottomHeight));
                    
                    using (var bottomMinRGB = new Mat())
                    using (var bottomBinary = new Mat())
                    using (var colSums = new Mat())
                    {
                        var bottomChannels = Cv2.Split(bottomMatTemp);
                        if (bottomChannels.Length == 3)
                        {
                            using var minBG = new Mat();
                            Cv2.Min(bottomChannels[0], bottomChannels[1], minBG);
                            Cv2.Min(minBG, bottomChannels[2], bottomMinRGB);
                            
                            Cv2.Threshold(bottomMinRGB, bottomBinary, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);
                            
                            // Reduce to a single row containing the sum of white pixels for each column
                            Cv2.Reduce(bottomBinary, colSums, ReduceDimension.Row, ReduceTypes.Sum, MatType.CV_32S);
                            
                            int minSearchX = (int)(safeWidth * 0.25);
                            int maxSearchX = (int)(safeWidth * 0.75);
                            int maxGapWidth = 0;
                            int currentGapStart = -1;
                            int bestGapCenter = splitX - hMargin;
                            int noiseThreshold = 255 * 3; // Allow up to 3 noise pixels per column

                            for (int x = minSearchX; x < maxSearchX; x++)
                            {
                                int sum = colSums.At<int>(0, x);
                                if (sum <= noiseThreshold)
                                {
                                    if (currentGapStart == -1) currentGapStart = x;
                                }
                                else
                                {
                                    if (currentGapStart != -1)
                                    {
                                        int gapWidth = x - currentGapStart;
                                        if (gapWidth > maxGapWidth)
                                        {
                                            maxGapWidth = gapWidth;
                                            bestGapCenter = currentGapStart + (gapWidth / 2);
                                        }
                                        currentGapStart = -1;
                                    }
                                }
                            }
                            
                            if (currentGapStart != -1)
                            {
                                int gapWidth = maxSearchX - currentGapStart;
                                if (gapWidth > maxGapWidth)
                                {
                                    maxGapWidth = gapWidth;
                                    bestGapCenter = currentGapStart + (gapWidth / 2);
                                }
                            }
                            
                            if (maxGapWidth > safeWidth * 0.02) // At least 2% of image width gap
                            {
                                splitX = bestGapCenter + hMargin;
                                _logger.LogInformation($"Dynamically found vertical column gap for split at X:{splitX} (Gap Width: {maxGapWidth})");
                            }
                        }
                    }

                    using var bottomLeftMat = new Mat(srcMat, new OpenCvSharp.Rect(hMargin, bottomY, splitX - hMargin, bottomHeight));
                    using var bottomRightMat = new Mat(srcMat, new OpenCvSharp.Rect(splitX, bottomY, width - splitX - hMargin, bottomHeight));

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
                    var modelLines = modelText.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToList();
                    
                    // Filter out barcode noise (barcode noise is usually repetitive 'I', 'L', 'l', '|' with few distinct chars)
                    // A valid model line should have a good mix of alphanumerics and be reasonably long
                    var validModelLines = modelLines.Where(l => l.Length >= 4 && l.Distinct().Count() >= 4 && Regex.IsMatch(l, @"[A-HJ-NP-Z0-9]{3,}")).ToList();
                    string model = validModelLines.FirstOrDefault() ?? "";
                    
                    if (!string.IsNullOrEmpty(model))
                    {
                        result.ExtractedFields.Add(new OcrField { Key = "Model", Value = model, Confidence = new OcrConfidence { Percentage = 90 } });
                    }

                    // 4. Process BOTTOM RIGHT for Color EXACTLY
                    string colorText = RunSimpleOcr(bottomRightMat);
                    var colorLines = colorText.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToList();
                    
                    // Filter out barcode noise
                    var validColorLines = colorLines.Where(l => l.Length >= 3 && Regex.IsMatch(l, @"[a-zA-Z]{3,}") && !Regex.IsMatch(l, @"^[lI1\|\s]+$")).ToList();
                    string color = validColorLines.LastOrDefault(l => l.Length > 3) ?? validColorLines.LastOrDefault() ?? "";
                    
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

                // Pass 2: MinRGB without Morphological Barcode Removal (Relying on C# Regex filtering to ignore barcode)
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
                        
                        // Barcode removal by morphology is risky (destroys thin characters like 'I' in 'ID').
                        // We rely on Regex filtering in the extraction step instead.
                        
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
