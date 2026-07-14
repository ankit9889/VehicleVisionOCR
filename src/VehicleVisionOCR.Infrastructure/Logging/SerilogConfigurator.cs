using Serilog;
using Serilog.Events;
using System.IO;

namespace VehicleVisionOCR.Infrastructure.Logging
{
    public static class SerilogConfigurator
    {
        public static void ConfigureLogging(string baseDirectory)
        {
            var logPath = Path.Combine(baseDirectory, "logs", "log-.txt");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Async(a => a.File(
                    path: logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
                .CreateLogger();
        }
    }
}
