using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VehicleVisionOCR.OCR.Core.Enums;
using VehicleVisionOCR.OCR.Core.Events;
using VehicleVisionOCR.OCR.Core.Models;

namespace VehicleVisionOCR.OCR.Core.Interfaces
{
    // ----------------------------------------------------
    // PREPROCESSING PIPELINE INTERFACES (Strategy Pattern)
    // ----------------------------------------------------
    public interface IImagePreprocessor
    {
        Task<byte[]> ProcessAsync(byte[] image, ProcessingProfile profile);
    }

    public interface IImageEnhancer : IImagePreprocessor { }
    public interface IImageDeskew : IImagePreprocessor { }
    public interface IImageCropper : IImagePreprocessor { }
    public interface IImageNoiseReducer : IImagePreprocessor { }
    public interface IImageThreshold : IImagePreprocessor { }

    public interface IImagePipeline
    {
        void AddProcessor(IImagePreprocessor processor);
        Task<byte[]> ExecuteAsync(byte[] image, ProcessingProfile profile);
    }

    // ----------------------------------------------------
    // ENGINE & PLUGIN INTERFACES
    // ----------------------------------------------------
    public interface IOcrEngine
    {
        string EngineName { get; }
        Task<OcrResultData> ProcessImageAsync(byte[] imageData);
    }

    public interface IOcrProvider
    {
        IOcrEngine CreateEngine();
    }

    public interface IOcrPlugin
    {
        string PluginId { get; }
        string PluginName { get; }
        string Version { get; }
        OcrEngineType SupportedEngine { get; }
        IOcrProvider CreateProvider();
    }

    // ----------------------------------------------------
    // FACTORY & MANAGER INTERFACES
    // ----------------------------------------------------
    public interface IOcrFactory
    {
        IOcrEngine CreateEngine(OcrEngineType engineType);
    }

    public interface IOcrManager
    {
        event EventHandler<ImageLoadedEventArgs> OnImageLoaded;
        event EventHandler<PreprocessingStartedEventArgs> OnPreprocessingStarted;
        event EventHandler<PreprocessingCompletedEventArgs> OnPreprocessingCompleted;
        event EventHandler<OcrStartedEventArgs> OnOcrStarted;
        event EventHandler<OcrCompletedEventArgs> OnOcrCompleted;
        event EventHandler<ValidationCompletedEventArgs> OnValidationCompleted;
        event EventHandler<ProcessingFailedEventArgs> OnProcessingFailed;

        Task<OcrResponse> ProcessAsync(OcrRequest request, OcrEngineType preferredEngine = OcrEngineType.Tesseract);
    }
}
