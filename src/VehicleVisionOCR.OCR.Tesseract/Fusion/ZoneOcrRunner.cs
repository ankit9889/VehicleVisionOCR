using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using OpenCvSharp;
using Tesseract;
using VehicleVisionOCR.Domain.Vision.Interfaces;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.OCR.Tesseract.Fusion
{
    public class ZoneOcrRunner : IZoneOcrRunner
    {
        private readonly TesseractObjectPool _pool;

        public ZoneOcrRunner(TesseractObjectPool pool)
        {
            _pool = pool;
        }

        public async Task<List<OcrObservation>> RunOcrPassesAsync(byte[] imageBytes, OcrProfileConfig config)
        {
            var tasks = new List<Task<OcrObservation>>();

            // Generate parallel tasks for every combination of Scale, PSM, and Preprocessing
            foreach (var scale in config.Scales)
            {
                foreach (var psm in config.PageSegmentationModes)
                {
                    foreach (var prep in config.PreprocessingPipelines)
                    {
                        tasks.Add(Task.Run(() => ExecuteOcrPass(imageBytes, config, scale, psm, prep)));
                    }
                }
            }

            var results = await Task.WhenAll(tasks);
            
            var validObservations = new List<OcrObservation>();
            foreach(var res in results)
            {
                if (res != null) validObservations.Add(res);
            }
            
            return validObservations;
        }

        private OcrObservation ExecuteOcrPass(byte[] rawBytes, OcrProfileConfig config, double scale, int psm, string preprocessingMethod)
        {
            var sw = Stopwatch.StartNew();
            var observation = new OcrObservation
            {
                PageSegmentationMode = psm,
                Scale = scale,
                PreprocessingMethod = preprocessingMethod
            };

            // 1. Preprocess and Scale the Image (Using OpenCV)
            using var srcMat = Cv2.ImDecode(rawBytes, ImreadModes.Grayscale);
            if (srcMat.Empty()) return null;

            using var processedMat = ApplyPreprocessing(srcMat, scale, preprocessingMethod);
            
            // Convert to byte array for Tesseract Pix load (MemoryStream)
            byte[] processedBytes = processedMat.ImEncode(".png");

            // 2. Borrow Tesseract instance and Execute
            var engine = _pool.BorrowAsync().GetAwaiter().GetResult();
            try
            {
                engine.DefaultPageSegMode = (PageSegMode)psm;
                
                if (!string.IsNullOrEmpty(config.WhitelistedCharacters))
                {
                    engine.SetVariable("tessedit_char_whitelist", config.WhitelistedCharacters);
                }

                using var ms = new MemoryStream(processedBytes);
                using var pix = Pix.LoadFromMemory(ms.ToArray());
                using var page = engine.Process(pix);

                observation.RawText = page.GetText()?.Trim();
                observation.AverageConfidence = page.GetMeanConfidence();

                // 3. Extract Character-Level Evidence using Iterator
                int currentLine = 0;
                int currentWord = 0;

                using var iter = page.GetIterator();
                iter.Begin();
                do
                {
                    if (iter.IsAtBeginningOf(PageIteratorLevel.TextLine)) currentLine++;
                    if (iter.IsAtBeginningOf(PageIteratorLevel.Word)) currentWord++;

                    if (iter.TryGetBoundingBox(PageIteratorLevel.Symbol, out var bounds))
                    {
                        string symbol = iter.GetText(PageIteratorLevel.Symbol);
                        if (!string.IsNullOrEmpty(symbol) && symbol.Length > 0)
                        {
                            // Downscale coordinates back to 1x equivalent so all passes align spatially
                            int origX = (int)(bounds.X1 / scale);
                            int origY = (int)(bounds.Y1 / scale);
                            int origW = (int)(bounds.Width / scale);
                            int origH = (int)(bounds.Height / scale);

                            var evidence = new CharacterEvidence
                            {
                                Character = symbol[0],
                                Confidence = iter.GetConfidence(PageIteratorLevel.Symbol),
                                X = origX,
                                Y = origY,
                                Width = origW,
                                Height = origH,
                                SourcePassId = observation.PassId,
                                SourcePageSegmentationMode = psm,
                                SourceScale = scale,
                                SourcePreprocessing = preprocessingMethod,
                                LineIndex = currentLine,
                                WordIndex = currentWord
                            };

                            // Try to get ChoiceIterator safely as optional evidence
                            try
                            {
                                using var choiceIter = iter.GetChoiceIterator();
                                if (choiceIter != null)
                                {
                                    do
                                    {
                                        string choiceText = choiceIter.GetText();
                                        if (!string.IsNullOrEmpty(choiceText) && choiceText.Length > 0)
                                        {
                                            evidence.Alternatives.Add(new CharacterChoice
                                            {
                                                Character = choiceText[0],
                                                Confidence = choiceIter.GetConfidence()
                                            });
                                        }
                                    } while (choiceIter.Next());
                                }
                            }
                            catch
                            {
                                // ChoiceIterator is treated as an optional evidence source.
                                // If it throws due to engine version/support, we silently ignore and proceed.
                            }

                            observation.Characters.Add(evidence);
                        }
                    }
                } while (iter.Next(PageIteratorLevel.Symbol));
            }
            catch (Exception ex)
            {
                // In production, log this exception. We return null to avoid failing the entire WhenAll block.
                Debug.WriteLine($"OCR Pass Failed: {ex.Message}");
                return null;
            }
            finally
            {
                _pool.Return(engine);
            }

            sw.Stop();
            observation.ExecutionTime = sw.Elapsed;

            return observation;
        }

        private Mat ApplyPreprocessing(Mat src, double scale, string method)
        {
            var result = new Mat();
            
            // Apply scale
            if (System.Math.Abs(scale - 1.0) > 0.01)
            {
                Cv2.Resize(src, result, new OpenCvSharp.Size(0, 0), scale, scale, InterpolationFlags.Cubic);
            }
            else
            {
                src.CopyTo(result);
            }

            switch (method)
            {
                case "CLAHE":
                    using (var clahe = Cv2.CreateCLAHE(2.0, new OpenCvSharp.Size(8, 8)))
                    {
                        clahe.Apply(result, result);
                    }
                    break;
                case "Adaptive":
                    Cv2.AdaptiveThreshold(result, result, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 11, 2);
                    break;
                case "Binary":
                    Cv2.Threshold(result, result, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                    break;
                case "Original":
                default:
                    // do nothing
                    break;
            }

            return result;
        }
    }
}
