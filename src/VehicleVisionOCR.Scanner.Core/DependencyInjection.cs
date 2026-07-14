using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using VehicleVisionOCR.Scanner.Core.Factory;
using VehicleVisionOCR.Scanner.Core.Interfaces;
using VehicleVisionOCR.Scanner.Core.Manager;
using VehicleVisionOCR.Scanner.Core.Plugins;

namespace VehicleVisionOCR.Scanner.Core
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddScannerFramework(this IServiceCollection services, string pluginsPath = "")
        {
            // Register Plugin Loader
            services.AddSingleton<IScannerPluginLoader, ScannerPluginLoader>();

            // Register core components
            services.AddSingleton<IScannerFactory, ScannerFactory>();
            services.AddSingleton<IScannerManager, ScannerManager>();

            // If we want to pre-load plugins at startup and register them in DI
            // services.AddSingleton<IEnumerable<IScannerPlugin>>(sp => 
            // {
            //     var loader = sp.GetRequiredService<IScannerPluginLoader>();
            //     var path = string.IsNullOrEmpty(pluginsPath) ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins") : pluginsPath;
            //     return loader.LoadPlugins(path);
            // });

            return services;
        }
    }
}
