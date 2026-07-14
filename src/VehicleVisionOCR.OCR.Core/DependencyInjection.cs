using Microsoft.Extensions.DependencyInjection;
using VehicleVisionOCR.OCR.Core.Factory;
using VehicleVisionOCR.OCR.Core.Interfaces;
using VehicleVisionOCR.OCR.Core.Manager;
using VehicleVisionOCR.OCR.Core.Pipeline;
using VehicleVisionOCR.OCR.Core.Plugins;

namespace VehicleVisionOCR.OCR.Core
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddOcrFramework(this IServiceCollection services)
        {
            // Register Plugin Loader
            services.AddSingleton<IOcrPluginLoader, OcrPluginLoader>();

            // Register Pipeline
            services.AddTransient<IImagePipeline, ImagePipeline>();

            // Register core components
            services.AddSingleton<IOcrFactory, OcrFactory>();
            services.AddSingleton<IOcrManager, OcrManager>();

            return services;
        }
    }
}
