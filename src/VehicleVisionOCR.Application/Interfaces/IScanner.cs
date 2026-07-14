using System;
using System.Threading.Tasks;
using VehicleVisionOCR.Domain.Entities;

namespace VehicleVisionOCR.Application.Interfaces
{
    /// <summary>
    /// Base interface to be implemented by scanner plugins (e.g., ZebraWindows, DataWedge).
    /// </summary>
    public interface IScanner
    {
        string PluginName { get; }
        bool IsConnected { get; }
        
        Task<bool> InitializeAsync(string connectionString);
        Task DisconnectAsync();
        Task TriggerScanAsync();

        event EventHandler<string> OnRawBarcodeScanned;
        event EventHandler<byte[]> OnImageCaptured;
    }
}
