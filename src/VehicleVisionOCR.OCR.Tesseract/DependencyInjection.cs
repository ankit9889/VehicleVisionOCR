using Microsoft.Extensions.DependencyInjection;
using VehicleVisionOCR.OCR.Core.Interfaces;
using VehicleVisionOCR.OCR.Tesseract.Preprocessors;

namespace VehicleVisionOCR.OCR.Tesseract
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddTesseractOcrPlugin(this IServiceCollection services)
        {
            // Register Plugin (For dynamic discovery by the framework)
            services.AddSingleton<IOcrPlugin, TesseractOcrPlugin>();

            // Register OpenCV Preprocessors so they can be injected into the Pipeline
            services.AddTransient<IImageDeskew, ImageDeskew>();
            services.AddTransient<IImageNoiseReducer, NoiseReducer>();
            services.AddTransient<IImageThreshold, ThresholdProcessor>();
            services.AddTransient<IImageEnhancer, SharpenProcessor>();
            services.AddTransient<IImageEnhancer, ContrastProcessor>();
            services.AddTransient<IImageCropper, ImageCropper>();
            
            // Register all as IImagePreprocessor for the pipeline
            services.AddTransient<IImagePreprocessor, ImageDeskew>();
            services.AddTransient<IImagePreprocessor, NoiseReducer>();
            services.AddTransient<IImagePreprocessor, ThresholdProcessor>();
            services.AddTransient<IImagePreprocessor, SharpenProcessor>();
            services.AddTransient<IImagePreprocessor, ContrastProcessor>();
            services.AddTransient<IImagePreprocessor, ImageCropper>();
            services.AddTransient<IImagePreprocessor, ImageRotation>();
            services.AddTransient<IImagePreprocessor, PerspectiveCorrection>();

            return services;
        }
    }
}
