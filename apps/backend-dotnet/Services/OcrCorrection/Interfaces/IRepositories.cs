using System.Collections.Generic;
using System.Threading.Tasks;

namespace VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces
{
    public interface IWmiRepository
    {
        Task<IEnumerable<string>> GetActiveWmiPrefixesAsync();
    }

    public interface IColorRepository
    {
        Task<IEnumerable<string>> GetActiveColorsAsync();
    }
    
    // Mocks for DI resolution
    public class MockWmiRepository : IWmiRepository
    {
        public Task<IEnumerable<string>> GetActiveWmiPrefixesAsync()
            => Task.FromResult<IEnumerable<string>>(new[] { "LB8TC", "ME4MC", "NE5LD", "A2S3D" });
    }
    
    public class MockColorRepository : IColorRepository
    {
        public Task<IEnumerable<string>> GetActiveColorsAsync()
            => Task.FromResult<IEnumerable<string>>(new[] { "BLACK", "WHITE", "RED", "BLUE", "SILVER", "GREY", "DEEP BLUE METALLIC", "LUNAR SILVER METALLIC" });
    }
}
