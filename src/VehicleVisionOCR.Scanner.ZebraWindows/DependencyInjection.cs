using Microsoft.Extensions.DependencyInjection;
using VehicleVisionOCR.Scanner.Core.Interfaces;

namespace VehicleVisionOCR.Scanner.ZebraWindows
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddZebraScannerPlugin(this IServiceCollection services)
        {
            // Register the plugin itself so the ScannerFramework can discover it if scanning the DI container
            services.AddSingleton<IScannerPlugin, ZebraScannerPlugin>();
            
            return services;
        }
    }
}
