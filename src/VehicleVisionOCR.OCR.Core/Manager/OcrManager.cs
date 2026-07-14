using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VehicleVisionOCR.OCR.Core.Enums;
using VehicleVisionOCR.OCR.Core.Events;
using VehicleVisionOCR.OCR.Core.Interfaces;
using VehicleVisionOCR.OCR.Core.Models;

namespace VehicleVisionOCR.OCR.Core.Manager
{
    public class OcrManager : IOcrManager
    {
        private readonly IOcrFactory _ocrFactory;
        private readonly IImagePipeline _imagePipeline;
        private readonly ILogger<OcrManager> _logger;

        public event EventHandler<ImageLoadedEventArgs>? OnImageLoaded;
        public event EventHandler<PreprocessingStartedEventArgs>? OnPreprocessingStarted;
        public event EventHandler<PreprocessingCompletedEventArgs>? OnPreprocessingCompleted;
        public event EventHandler<OcrStartedEventArgs>? OnOcrStarted;
        public event EventHandler<OcrCompletedEventArgs>? OnOcrCompleted;
        public event EventHandler<ValidationCompletedEventArgs>? OnValidationCompleted;
        public event EventHandler<ProcessingFailedEventArgs>? OnProcessingFailed;

        public OcrManager(
            IOcrFactory ocrFactory, 
            IImagePipeline imagePipeline, 
            ILogger<OcrManager> logger)
        {
            _ocrFactory = ocrFactory;
            _imagePipeline = imagePipeline;
            _logger = logger;
        }

        public async Task<OcrResponse> ProcessAsync(OcrRequest request, OcrEngineType preferredEngine = OcrEngineType.Tesseract)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = new OcrResponse { RequestId = request.RequestId, Status = OcrStatus.Pending };

            try
            {
                // 1. Image Loaded
                OnImageLoaded?.Invoke(this, new ImageLoadedEventArgs { RequestId = request.RequestId });
                
                // 2. Preprocessing
                OnPreprocessingStarted?.Invoke(this, new PreprocessingStartedEventArgs { RequestId = request.RequestId });
                response.Status = OcrStatus.Processing;
                
                byte[] processedImage = await _imagePipeline.ExecuteAsync(request.ImageData, request.Profile);
                
                OnPreprocessingCompleted?.Invoke(this, new PreprocessingCompletedEventArgs 
                { 
                    RequestId = request.RequestId,
                    ProcessedImageData = processedImage
                });

                // 3. OCR Engine Selection & Execution
                var engine = _ocrFactory.CreateEngine(preferredEngine);
                
                OnOcrStarted?.Invoke(this, new OcrStartedEventArgs 
                { 
                    RequestId = request.RequestId,
                    EngineName = engine.EngineName
                });

                var ocrResult = await engine.ProcessImageAsync(processedImage);
                
                response.Result = ocrResult;
                response.Status = OcrStatus.Success;

                OnOcrCompleted?.Invoke(this, new OcrCompletedEventArgs 
                { 
                    RequestId = request.RequestId,
                    Response = response
                });

                // 4. Validation (Simplified for Framework demonstration)
                bool isValid = ocrResult.OverallConfidence.IsReliable;
                OnValidationCompleted?.Invoke(this, new ValidationCompletedEventArgs 
                { 
                    RequestId = request.RequestId,
                    IsValid = isValid
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"OCR processing failed for request {request.RequestId}");
                response.Status = OcrStatus.Failed;
                response.ErrorMessage = ex.Message;

                OnProcessingFailed?.Invoke(this, new ProcessingFailedEventArgs 
                { 
                    RequestId = request.RequestId,
                    ErrorMessage = ex.Message,
                    Exception = ex
                });
            }
            finally
            {
                stopwatch.Stop();
                response.ProcessingTime = stopwatch.Elapsed;
            }

            return response;
        }
    }
}
