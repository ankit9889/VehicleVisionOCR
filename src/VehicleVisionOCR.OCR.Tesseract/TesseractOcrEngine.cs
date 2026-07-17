using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tesseract;
using OpenCvSharp;
using ZXing;
using ZXing.Common;
using VehicleVisionOCR.OCR.Core.Interfaces;
using VehicleVisionOCR.OCR.Core.Models;
using VehicleVisionOCR.OCR.Tesseract.Exceptions;

namespace VehicleVisionOCR.OCR.Tesseract
{
    public class TesseractOcrEngine : IOcrEngine
    {
        private readonly ILogger<TesseractOcrEngine> _logger;
        private readonly string _tessDataPath;
        private readonly string _language;
        private readonly Dictionary<string, string> _ocrCorrections = new Dictionary<string, string>();

        public string EngineName => "Advanced Vision Engine 2026";

        private static TesseractEngine _sharedEngine;
        private static readonly object _engineLock = new object();

        public TesseractOcrEngine(ILogger<TesseractOcrEngine> logger, Microsoft.Extensions.Configuration.IConfiguration configuration = null)
        {
            _logger = logger;
            _tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
            _language = "eng"; 
            
            if (configuration != null)
            {
                var section = configuration.GetSection("OcrCorrections");
                foreach (var child in section.GetChildren())
                {
                    if (!string.IsNullOrEmpty(child.Key) && !string.IsNullOrEmpty(child.Value))
                    {
                        _ocrCorrections[child.Key] = child.Value;
                    }
                }
            }
        }

        public async Task<OcrResultData> ProcessImageAsync(byte[] imageData)
        {
            return await ProcessImageAsync(imageData, false);
        }

        public async Task<OcrResultData> ProcessImageAsync(byte[] imageData, bool isStructuredCrop)
        {
            if (imageData == null || imageData.Length == 0)
                throw new ArgumentException("Image data is null or empty.", nameof(imageData));

            return await Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(_tessDataPath))
                    {
                        _logger.LogWarning($"Tesseract trained data path not found at: {_tessDataPath}.");
                        Directory.CreateDirectory(_tessDataPath);
                    }

                    _logger.LogInformation($"Starting Advanced OCR on image ({imageData.Length} bytes).");

                    // 1. Load image via OpenCV
                    using var originalMat = Cv2.ImDecode(imageData, ImreadModes.Color);
                    using var srcMat = new Mat();

                    
                    // Resize to speed up processing and standardize threshold parameters
                    const int maxWidth = 1600;
                    if (originalMat.Width > maxWidth)
                    {
                        double scale = (double)maxWidth / originalMat.Width;
                        Cv2.Resize(originalMat, srcMat, new OpenCvSharp.Size(maxWidth, (int)(originalMat.Height * scale)));
                    }
                    else
                    {
                        originalMat.CopyTo(srcMat);
                    }
                    
                    // 2. ZXing Barcode Verification
                    string decodedBarcode = DecodeBarcode(srcMat);
                    
                    if (!string.IsNullOrEmpty(decodedBarcode) && decodedBarcode.Contains(" "))
                    {
                        // Clean up decoded barcode (extract VIN only if it appended something like 1SK1LD10)
                        var parts = decodedBarcode.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        var vinPart = parts.FirstOrDefault(p => p.Length >= 11 && p.Length <= 17);
                        if (vinPart != null)
                        {
                            decodedBarcode = vinPart;
                            _logger.LogInformation($"ZXing barcode cleaned from multi-part scan to: {decodedBarcode}");
                        }
                    }

                    // 2.5 Upscale for Tesseract if image is too small (helps with tiny/thin text like colors)
                    using var tesseractMat = new Mat();
                    const int minWidth = 1200; // Increased to 1200 to guarantee clear upscaling
                    if (srcMat.Width > 0 && srcMat.Width < minWidth)
                    {
                        double scale = (double)minWidth / srcMat.Width;
                        Cv2.Resize(srcMat, tesseractMat, new OpenCvSharp.Size(minWidth, (int)(srcMat.Height * scale)), 0, 0, InterpolationFlags.Cubic);
                    }
                    else
                    {
                        srcMat.CopyTo(tesseractMat);
                    }
                    
                    // 3. Preprocessing multiple Mats
                    var preprocessedMats = GeneratePreprocessedVariations(tesseractMat);

                    // 4. Multiple OCR Passes
                    var allCandidates = new List<VinCandidate>();
                    var allRawTexts = new List<string>();
                    
                    lock (_engineLock)
                    {
                        if (_sharedEngine == null)
                        {
                            _sharedEngine = new TesseractEngine(_tessDataPath, _language, EngineMode.Default);
                        }

                        TesseractEngine engineToUse = _sharedEngine;
                        bool disposeEngine = false;

                        if (isStructuredCrop)
                        {
                            // Create an isolated engine ONCE for VIN processing with aggressive whitelist
                            engineToUse = new TesseractEngine(_tessDataPath, _language, EngineMode.LstmOnly);
                            engineToUse.SetVariable("tessedit_char_whitelist", "0123456789ABCDEFGHJKLMNPRSTUVWXYZ ");
                            disposeEngine = true;
                        }

                        try
                        {
                            foreach (var kvp in preprocessedMats)
                            {
                                var mat = kvp.Value;
                                var passName = kvp.Key;
                                
                                byte[] matBytes = mat.ToBytes(".png");
                                using var pix = Pix.LoadFromMemory(matBytes);
                                
                                // Implement PSM Ensemble (PSM 3) for structured crops
                                // We strictly AVOID SingleLine (PSM 7) and SingleBlock (PSM 6) because the "Top" crop often contains 2 lines (Header + VIN), and forcing them into a block causes vertical hallucinations like MEG6.
                                PageSegMode[] psms = isStructuredCrop 
                                    ? new[] { PageSegMode.Auto } 
                                    : new[] { (passName == "OriginalGray" ? PageSegMode.Auto : PageSegMode.SparseText) };

                                foreach (var psm in psms)
                                {
                                    using var page = engineToUse.Process(pix, psm);
                                    
                                    string text = page.GetText();
                                    allRawTexts.Add(text);
                                    
                                    var detectedTexts = new List<DetectedText>();
                                    using (var iter = page.GetIterator())
                                    {
                                        iter.Begin();
                                        do
                                        {
                                            var word = iter.GetText(PageIteratorLevel.Word);
                                            if (!string.IsNullOrWhiteSpace(word) && iter.TryGetBoundingBox(PageIteratorLevel.Word, out global::Tesseract.Rect bounds))
                                            {
                                                detectedTexts.Add(new DetectedText
                                                {
                                                    Text = word,
                                                    X = bounds.X1,
                                                    Y = bounds.Y1,
                                                    Width = bounds.Width,
                                                    Height = bounds.Height,
                                                    Confidence = new OcrConfidence { Percentage = iter.GetConfidence(PageIteratorLevel.Word) * 100 }
                                                });
                                            }
                                        } while (iter.Next(PageIteratorLevel.Word));
                                    }

                                    // Extract candidates from this pass
                                    ExtractCandidatesFromPass(text, detectedTexts, allCandidates, $"{passName}_{psm}", isStructuredCrop);
                                }
                            
                            mat.Dispose();
                            
                        } // close foreach
                        }
                        finally
                        {
                            if (disposeEngine && engineToUse != null)
                            {
                                engineToUse.Dispose();
                            }
                        }
                    } // close lock

                    // 5. Candidate Scoring (Get top ensemble candidates)
                    var topCandidates = ScoreAndSelectTopCandidates(allCandidates, decodedBarcode, topN: 5);
                    var bestCandidate = topCandidates.FirstOrDefault();
                    
                    var result = new OcrResultData
                    {
                        RawText = string.Join("\n---\n", allRawTexts),
                        OverallConfidence = new OcrConfidence { Percentage = bestCandidate?.Score > 0 ? 99.0 : 50.0 },
                        ExtractedFields = new List<OcrField>()
                    };

                    if (bestCandidate != null)
                    {
                        double finalConfidence = (decodedBarcode == bestCandidate.Text) ? 99.8 : (bestCandidate.Score >= 40 ? 95.0 : (bestCandidate.Score >= 20 ? 85.0 : 65.0));
                        if (!string.IsNullOrEmpty(decodedBarcode) && decodedBarcode != bestCandidate.Text)
                        {
                            // If different, return both
                            result.ExtractedFields.Add(new OcrField { Key = "Barcode_Decoded", Value = decodedBarcode, Confidence = new OcrConfidence { Percentage = 99.9 } });
                        }
                        
                        // Lowered threshold to 70% so we don't return NULL for generic 15-char barcodes
                        if (finalConfidence >= 70.0)
                        {
                            // Output ensemble of candidates joined by pipe for the verification layer
                            string ensembleValue = string.Join("|", topCandidates.Select(c => c.Text).Distinct());

                            result.ExtractedFields.Add(new OcrField 
                            { 
                                Key = "VIN", 
                                Value = ensembleValue,
                                Confidence = new OcrConfidence { Percentage = finalConfidence }
                            });
                            // Keep backward compatibility for frontend (use the best one for UI display if needed)
                            result.ExtractedFields.Add(new OcrField { Key = "Barcode", Value = bestCandidate.Text, Confidence = new OcrConfidence { Percentage = finalConfidence } });
                        }
                        else
                        {
                            result.ExtractedFields.Add(new OcrField { Key = "VIN", Value = "NULL", Confidence = new OcrConfidence { Percentage = 0 } });
                        }
                    }
                    else if (!string.IsNullOrEmpty(decodedBarcode))
                    {
                        result.ExtractedFields.Add(new OcrField { Key = "VIN", Value = decodedBarcode, Confidence = new OcrConfidence { Percentage = 99.8 } });
                        result.ExtractedFields.Add(new OcrField { Key = "Barcode", Value = decodedBarcode, Confidence = new OcrConfidence { Percentage = 99.8 } });
                    }

                    // Re-run color and model on the combined raw text of ALL passes
                    ExtractColorAndModel(string.Join("\n", allRawTexts), result.ExtractedFields);

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Tesseract OCR processing failed.");
                    throw new TesseractOcrException("Failed to process image with Tesseract.", ex);
                }
            });
        }

        private string DecodeBarcode(Mat srcMat)
        {
            try
            {
                var reader = new ZXing.BarcodeReaderGeneric
                {
                    AutoRotate = true,
                    Options = new ZXing.Common.DecodingOptions
                    {
                        TryHarder = true,
                        PossibleFormats = null // Try all formats
                    }
                };
                
                using var gray = new Mat();
                Cv2.CvtColor(srcMat, gray, ColorConversionCodes.BGR2GRAY);

                // Add a white padding border (Quiet Zone) to help ZXing detect barcodes that are printed too close to the edge
                using var paddedGray = new Mat();
                Cv2.CopyMakeBorder(gray, paddedGray, 50, 50, 50, 50, BorderTypes.Constant, new Scalar(255));
                
                // Try 1: Raw Grayscale
                var result = TryDecodeMat(paddedGray, reader);
                if (result != null) return result;
                
                // Try 2: Scale UP 2x
                using var scaledUp = new Mat();
                Cv2.Resize(paddedGray, scaledUp, new OpenCvSharp.Size(0,0), 2.0, 2.0, InterpolationFlags.Cubic);
                result = TryDecodeMat(scaledUp, reader);
                if (result != null) return result;
                
                // Try 3: Gaussian Blur + Otsu Threshold
                using var blurred = new Mat();
                Cv2.GaussianBlur(paddedGray, blurred, new OpenCvSharp.Size(5, 5), 0);
                using var blurBin = new Mat();
                Cv2.Threshold(blurred, blurBin, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                result = TryDecodeMat(blurBin, reader);
                if (result != null) return result;

                // Try 4: CLAHE
                using var clahe = Cv2.CreateCLAHE(clipLimit: 2.0, tileGridSize: new OpenCvSharp.Size(8, 8));
                using var claheMat = new Mat();
                clahe.Apply(paddedGray, claheMat);
                result = TryDecodeMat(claheMat, reader);
                if (result != null) return result;
                
                // Try 5: Binary Threshold (Standard)
                using var binMat = new Mat();
                Cv2.Threshold(paddedGray, binMat, 128, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                result = TryDecodeMat(binMat, reader);
                if (result != null) return result;

                // Try 6: Adaptive Threshold
                using var adaptMat = new Mat();
                Cv2.AdaptiveThreshold(paddedGray, adaptMat, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 21, 10);
                result = TryDecodeMat(adaptMat, reader);
                if (result != null) return result;
                
                Console.WriteLine("[ZXing] FAILED to decode barcode from all image variations.");
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ZXing] EXCEPTION: {ex.Message}");
                _logger.LogWarning(ex, "ZXing barcode decode failed.");
                return string.Empty;
            }
        }

        private string TryDecodeMat(Mat mat, ZXing.BarcodeReaderGeneric reader)
        {
            try
            {
                int width = mat.Width;
                int height = mat.Height;
                byte[] pixelData = new byte[width * height];
                System.Runtime.InteropServices.Marshal.Copy(mat.Data, pixelData, 0, pixelData.Length);
                var source = new ZXing.RGBLuminanceSource(pixelData, width, height, ZXing.RGBLuminanceSource.BitmapFormat.Gray8);
                var result = reader.Decode(source);
                if (result != null && result.Text.Length >= 11)
                {
                    Console.WriteLine($"[ZXing] DECODED BARCODE: {result.Text}");
                    return result.Text;
                }
                else if (result != null)
                {
                    Console.WriteLine($"[ZXing] IGNORED SHORT BARCODE: {result.Text}");
                }
            }
            catch { }
            return null;
        }

        private Dictionary<string, Mat> GeneratePreprocessedVariations(Mat srcMat)
        {
            var dict = new Dictionary<string, Mat>();
            
            // 1. Original Grayscale
            var gray = new Mat();
            Cv2.CvtColor(srcMat, gray, ColorConversionCodes.BGR2GRAY);
            
            // Add a white padding border because Tesseract ignores text touching the edge of the image
            Cv2.CopyMakeBorder(gray, gray, 50, 50, 50, 50, BorderTypes.Constant, new OpenCvSharp.Scalar(255));
            
            dict.Add("OriginalGray", gray);
            
            // 2. CLAHE (Contrast Limited Adaptive Histogram Equalization)
            var clahe = Cv2.CreateCLAHE(2.0, new OpenCvSharp.Size(8, 8));
            var claheMat = new Mat();
            clahe.Apply(gray, claheMat);
            dict.Add("CLAHE", claheMat);
            
            // 3. Adaptive Threshold (Small Block for thin text)
            var adaptiveMat = new Mat();
            Cv2.AdaptiveThreshold(claheMat, adaptiveMat, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 11, 2);
            dict.Add("AdaptiveThreshold", adaptiveMat);

            // 4. Binary Threshold (Good for clean, well-lit text)
            var binaryMat = new Mat();
            Cv2.Threshold(gray, binaryMat, 128, 255, ThresholdTypes.Binary);
            dict.Add("BinaryThreshold", binaryMat);

            // 5. Adaptive Threshold (Large Block for THICK text like VINs)
            var adaptiveMat2 = new Mat();
            Cv2.AdaptiveThreshold(gray, adaptiveMat2, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 81, 2);
            dict.Add("AdaptiveLarge", adaptiveMat2);
            
            // 6. Blurred Pass (Helps when text is too close to the barcode lines)
            var blurredMat = new Mat();
            Cv2.GaussianBlur(gray, blurredMat, new OpenCvSharp.Size(5, 5), 0);
            var otsuBlurredMat = new Mat();
            Cv2.Threshold(blurredMat, otsuBlurredMat, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
            dict.Add("BlurredOtsu", otsuBlurredMat);

            // 7. Barcode Removal Pass (Erase vertical lines using horizontal morphology)
            var barcodeRemovalMat = new Mat();
            var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(7, 1));
            Cv2.MorphologyEx(binaryMat, barcodeRemovalMat, MorphTypes.Open, kernel);
            dict.Add("BarcodeRemoval", barcodeRemovalMat);

            // 8. Thickened Text Pass (Erosion thickens dark pixels/text on a light background)
            var thickenedMat = new Mat();
            using var thickenKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
            Cv2.Erode(gray, thickenedMat, thickenKernel);
            dict.Add("ErodedGray", thickenedMat);
            
            // 8.5 Thinned Text Pass (Dilation thins dark pixels on a light background) - Fixes bleeding ink (6 vs G)
            var thinnedMat = new Mat();
            using var thinKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
            Cv2.Dilate(gray, thinnedMat, thinKernel);
            // Apply Otsu to the thinned image to ensure sharp edges
            var thinnedOtsu = new Mat();
            Cv2.Threshold(thinnedMat, thinnedOtsu, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
            dict.Add("ThinnedText", thinnedOtsu);
            
            // 9. MinRGB Pass (Ultimate contrast for ANY colored text on white background)
            var channels = Cv2.Split(srcMat);
            if (channels.Length == 3)
            {
                var minBG = new Mat();
                Cv2.Min(channels[0], channels[1], minBG);
                var minRGB = new Mat();
                Cv2.Min(minBG, channels[2], minRGB);
                
                var minRgbOtsuMat = new Mat();
                Cv2.Threshold(minRGB, minRgbOtsuMat, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                dict.Add("MinRgbOtsu", minRgbOtsuMat);
            }
            
            return dict;
        }


        private void ExtractCandidatesFromPass(string rawText, List<DetectedText> detectedTexts, List<VinCandidate> allCandidates, string passName, bool isStructuredCrop)
        {
            // 1. Standard Regex pass on lines (prevent merging different lines into one giant false-positive string)
            var lines = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var barcodeRegex = new Regex(@"[A-Z0-9]{12,25}", RegexOptions.IgnoreCase);
            var exactVinRegex = new Regex(@"([A-Z0-9]{11,13}\d{4,9})", RegexOptions.IgnoreCase);

            foreach (var line in lines)
            {
                var cleanLine = Regex.Replace(line, @"[^A-Z0-9 ]+", "").ToUpperInvariant(); // Keep spaces to separate words
                
                // 1a. Test line with spaces removed (in case Tesseract inserted false spaces into a perfect VIN)
                var spacelessLine = cleanLine.Replace(" ", "");
                foreach (Match match in barcodeRegex.Matches(spacelessLine))
                {
                    string val = match.Value;
                    allCandidates.Add(new VinCandidate { Text = val, OriginalText = line, Source = "SpacelessLine", PassName = passName });
                }

                // 1b. Test line with spaces (standard)
                foreach (Match match in barcodeRegex.Matches(cleanLine))
                {
                    string val = match.Value.Replace(" ", "");
                    allCandidates.Add(new VinCandidate { Text = val, OriginalText = val, Source = "CleanLine", PassName = passName });
                }

                // Split by spaces so we evaluate distinct words, preventing cross-word merging
                var words = cleanLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    var squishedWord = Regex.Replace(word, @"[^A-Z0-9]+", "");
                    foreach (Match match in exactVinRegex.Matches(squishedWord))
                    {
                        string val = match.Groups[1].Value;
                        if (val.Length >= 14 && val.Length <= 20)
                        {
                            allCandidates.Add(new VinCandidate { Text = val, OriginalText = val, Source = "ExactVIN", PassName = passName });
                        }
                    }
                    
                    // Also run general barcode regex on the word
                    foreach (Match match in barcodeRegex.Matches(squishedWord))
                    {
                        allCandidates.Add(new VinCandidate { Text = match.Value, OriginalText = match.Value, Source = "UltraCleanWord", PassName = passName });
                    }
                }
            }

            // 3. Spatial lines pass
            if (detectedTexts.Any() && !isStructuredCrop)
            {
                var lineGroups = new List<List<DetectedText>>();
                foreach (var dt in detectedTexts.OrderBy(d => d.Y))
                {
                    var tolerance = Math.Max(10, dt.Height / 3);
                    var group = lineGroups.FirstOrDefault(g => Math.Abs(g.First().Y - dt.Y) < tolerance);
                    if (group != null) group.Add(dt);
                    else lineGroups.Add(new List<DetectedText> { dt });
                }

                // Find largest text
                double maxAvgHeight = lineGroups.Max(g => g.Average(w => w.Height));

                foreach (var group in lineGroups)
                {
                    var orderedWords = group.OrderBy(w => w.X).ToList();
                    var lineText = string.Join(" ", orderedWords.Select(w => w.Text));
                    var strippedLine = Regex.Replace(lineText.ToUpperInvariant(), @"[^A-Z0-9]+", "");
                    // Strip trailing barcode hallucination characters (often appended to the actual VIN)
                    strippedLine = Regex.Replace(strippedLine, @"[ILMNUVW\|]+$", "");

                    if (strippedLine.Length >= 10)
                    {
                        allCandidates.Add(new VinCandidate 
                        { 
                            Text = strippedLine, 
                            OriginalText = lineText, 
                            Source = "Spatial",
                            PassName = passName,
                            IsLargest = Math.Abs(group.Average(w => w.Height) - maxAvgHeight) < 5
                        });
                    }
                }
            }
        }

        private List<VinCandidate> ScoreAndSelectTopCandidates(List<VinCandidate> candidates, string decodedBarcode, int topN)
        {
            // Calculate consensus frequencies
            var frequencies = candidates.GroupBy(c => c.Text).ToDictionary(g => g.Key, g => g.Count());

            Console.WriteLine("--- ALL OCR CANDIDATES BEFORE SCORING ---");
            foreach (var cand in candidates)
            {
                Console.WriteLine($"Candidate: '{cand.Text}' | Source: {cand.Source} | Pass: {cand.PassName} | Original: '{cand.OriginalText}'");
                cand.Score = 0;

                // Length 16-17 is standard
                if (cand.Text.Length == 17)
                {
                    cand.Score += 25;
                }
                else if (cand.Text.Length == 16)
                {
                    cand.Score += 25; 
                }
                else if (cand.Text.Length >= 14 && cand.Text.Length <= 20)
                {
                    cand.Score += 20; // Valid generic barcode length
                }

                // Barcode bleed penalty (Tesseract reading barcode lines as I or L)
                if (System.Text.RegularExpressions.Regex.IsMatch(cand.Text, @"[IL]{4,}"))
                {
                    cand.Score -= 100;
                }

                // Severe Barcode Hallucination Penalty: if > 60% of chars are ILMNUVW (barcode line misreads)
                int barcodeCharCount = cand.Text.Count(c => "ILMNUVW1|".Contains(c));
                if ((double)barcodeCharCount / cand.Text.Length > 0.6)
                {
                    cand.Score -= 200;
                }

                // Barcode Noise Filter: Real VINs have high entropy (many different letters and numbers).
                // False barcode reads like IRITARRAEEETERIITE only have a few unique characters repeating.
                int uniqueCharsCount = cand.Text.Distinct().Count();
                if (cand.Text.Length >= 14 && uniqueCharsCount < 7)
                {
                    cand.Score -= 300; // Heavy penalty for repetitive noise
                }
                
                // Forbidden words (heavy penalty to prevent header lines from winning)
                if (cand.OriginalText.Contains("FRAME") || cand.OriginalText.Contains("NO") || cand.OriginalText.Contains("DATE"))
                {
                    cand.Score -= 100;
                }

                // Minor Forbidden words (only penalize if NOT a valid VIN length)
                if (cand.Text.Length != 17 && cand.Text.Length != 16 && cand.Text.Length != 14 && 
                   (cand.Text.Contains("2024") || cand.Text.Contains("2025") || cand.Text.Contains("2026") || 
                    cand.Text.Contains("MODEL") || cand.Text.Contains("CB")))
                {
                    cand.Score -= 50;
                }

                // Strict Regex (No I, O, Q allowed in standard VIN)
                if (Regex.IsMatch(cand.Text, @"^[A-HJ-NPR-Z0-9]{14,20}$"))
                {
                    cand.Score += 20;
                }

                // Largest text (only applies to spatial)
                if (cand.IsLargest)
                {
                    cand.Score += 25;
                }

                // Spaces penalty
                if (cand.OriginalText.Contains(" "))
                {
                    cand.Score -= 40;
                }

                // Lowercase penalty (checked on original before ToUpper)
                if (Regex.IsMatch(cand.OriginalText, @"[a-z]"))
                {
                    cand.Score -= 30;
                }

                // Reward standard OCR line engine over manual spatial weaving
                if (cand.Source == "ExactVIN" || cand.Source == "CleanLine" || cand.Source == "SpacelessLine")
                {
                    cand.Score += 30;
                }

                // Decoded barcode match boost
                if (!string.IsNullOrEmpty(decodedBarcode) && cand.Text == decodedBarcode)
                {
                    cand.Score += 100;
                }
                
                // Consensus Boost: Give a boost based on how many times this text was independently generated
                if (frequencies.TryGetValue(cand.Text, out int count) && count > 1)
                {
                    cand.Score += count * 10;
                }
                
                // Pass Source Tie-breaker: OriginalGray is usually the most reliable
                if (cand.PassName == "OriginalGray")
                {
                    cand.Score += 5;
                }
            }

            return candidates.OrderByDescending(c => c.Score).Where(c => c.Score > 0).GroupBy(c => c.Text).Select(g => g.First()).Take(topN).ToList();
        }

        private void ExtractColorAndModel(string rawText, List<OcrField> fields)
        {
            // Replaced the simple list with a broader list including finishes
            var colorRegex = new Regex(@"(?:[A-Z]+\s+)?(?:[A-Z]+\s+)?(BLACK|WHITE|RED|BLUE|GREY|GRAY|SILVER|GREEN|YELLOW|ORANGE|BROWN|PEARL|MICA|METALLIC)(?:\s+[A-Z]+)?", RegexOptions.IgnoreCase);
            var colorMatch = colorRegex.Match(rawText);

            if (colorMatch.Success)
            {
                var extractedColor = colorMatch.Value.ToUpperInvariant().Trim();
                

                fields.Add(new OcrField { Key = "Color", Value = extractedColor, Confidence = new OcrConfidence { Percentage = 80.0 } });
            }

            // Fixed Model Regex to support alphanumeric suffixes (e.g. CBF170S6 instead of failing on the 6)
            var modelRegex = new Regex(@"\b(CBF?\d+[A-Z0-9]*|Activa\w*|Dio\w*)\b", RegexOptions.IgnoreCase);
            var modelMatch = modelRegex.Match(rawText);

            if (modelMatch.Success)
            {
                fields.Add(new OcrField { Key = "Model", Value = modelMatch.Value.ToUpperInvariant(), Confidence = new OcrConfidence { Percentage = 85.0 } });
            }
        }
    }

    public class VinCandidate
    {
        public string Text { get; set; } = string.Empty;
        public string OriginalText { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string PassName { get; set; } = string.Empty;
        public bool IsLargest { get; set; }
        public int Score { get; set; }
    }
}
