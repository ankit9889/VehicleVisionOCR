using System;
using System.Threading.Tasks;
using VehicleVisionOCR.Domain.Entities;

namespace VehicleVisionOCR.Application.Interfaces
{
    /// <summary>
    /// Manages multiple scanner plugins and abstracts UI communication.
    /// </summary>
    public interface IScannerManager
    {
        /// <summary>
        /// Gets the currently active scanner device.
        /// </summary>
        ScannerDevice? CurrentScanner { get; }

        Task ConnectAsync(string pluginName, string portOrId);
        Task DisconnectAsync();
        
        /// <summary>
        /// Fires when a scan is received from any active plugin.
        /// </summary>
        event EventHandler<VehicleScan> OnScanReceived;
    }
}
