using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using VehicleVisionOCR.OCR.Core.Interfaces;

namespace VehicleVisionOCR.OCR.Core.Plugins
{
    public interface IOcrPluginLoader
    {
        IEnumerable<IOcrPlugin> LoadPlugins(string pluginsDirectory);
    }

    public class OcrPluginLoader : IOcrPluginLoader
    {
        private readonly ILogger<OcrPluginLoader> _logger;

        public OcrPluginLoader(ILogger<OcrPluginLoader> logger)
        {
            _logger = logger;
        }

        public IEnumerable<IOcrPlugin> LoadPlugins(string pluginsDirectory)
        {
            var plugins = new List<IOcrPlugin>();

            if (!Directory.Exists(pluginsDirectory))
            {
                _logger.LogWarning($"OCR Plugins directory not found: {pluginsDirectory}");
                return plugins;
            }

            var dllFiles = Directory.GetFiles(pluginsDirectory, "*.dll");

            foreach (var file in dllFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file);
                    var pluginTypes = assembly.GetTypes()
                        .Where(t => typeof(IOcrPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    foreach (var type in pluginTypes)
                    {
                        if (Activator.CreateInstance(type) is IOcrPlugin plugin)
                        {
                            plugins.Add(plugin);
                            _logger.LogInformation($"Loaded OCR plugin: {plugin.PluginName} (Version: {plugin.Version})");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to load OCR plugin from {file}");
                }
            }

            return plugins;
        }
    }
}
