using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using VehicleVisionOCR.Scanner.Core.Interfaces;

namespace VehicleVisionOCR.Scanner.Core.Plugins
{
    public interface IScannerPluginLoader
    {
        IEnumerable<IScannerPlugin> LoadPlugins(string pluginsDirectory);
    }

    public class ScannerPluginLoader : IScannerPluginLoader
    {
        private readonly ILogger<ScannerPluginLoader> _logger;

        public ScannerPluginLoader(ILogger<ScannerPluginLoader> logger)
        {
            _logger = logger;
        }

        public IEnumerable<IScannerPlugin> LoadPlugins(string pluginsDirectory)
        {
            var plugins = new List<IScannerPlugin>();

            if (!Directory.Exists(pluginsDirectory))
            {
                _logger.LogWarning($"Plugins directory not found: {pluginsDirectory}");
                return plugins;
            }

            var dllFiles = Directory.GetFiles(pluginsDirectory, "*.dll");

            foreach (var file in dllFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file);
                    var pluginTypes = assembly.GetTypes()
                        .Where(t => typeof(IScannerPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    foreach (var type in pluginTypes)
                    {
                        if (Activator.CreateInstance(type) is IScannerPlugin plugin)
                        {
                            plugins.Add(plugin);
                            _logger.LogInformation($"Loaded plugin: {plugin.PluginName} (Version: {plugin.Version})");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to load plugin from {file}");
                }
            }

            return plugins;
        }
    }
}
