using VehicleVisionOCR.Domain.VIN.Models;

namespace VehicleVisionOCR.Domain.VIN.Interfaces
{
    public interface IWmiKnowledgeProvider
    {
        bool IsValidWmi(string wmi);
        ManufacturerData GetManufacturerData(string wmi);
    }
}
