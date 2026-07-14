using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VehicleVisionOCR.Scanner.Core.Enums;
using VehicleVisionOCR.Scanner.Core.Interfaces;

namespace VehicleVisionOCR.Scanner.ZebraWindows
{
    public class ZebraScannerPlugin : IScannerPlugin
    {
        private readonly ILogger<ZebraScannerProvider> _providerLogger;

        public ZebraScannerPlugin(ILogger<ZebraScannerProvider> providerLogger)
        {
            _providerLogger = providerLogger;
        }

        // Required default constructor for reflection-based plugin loading
        public ZebraScannerPlugin()
        {
            // The actual provider logger will be resolved via DI in the provider factory
            _providerLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ZebraScannerProvider>.Instance;
        }

        public string PluginId => "Zebra.CoreScanner.Windows";
        public string PluginName => "Zebra CoreScanner SDK for Windows";
        public string Version => "1.0.0";
        public ScannerBrand SupportedBrand => ScannerBrand.Zebra;

        public IScannerProvider CreateProvider()
        {
            return new ZebraScannerProvider(_providerLogger);
        }
    }
}
