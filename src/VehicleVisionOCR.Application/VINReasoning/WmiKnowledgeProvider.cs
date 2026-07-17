using System.Collections.Generic;
using VehicleVisionOCR.Domain.VIN.Interfaces;
using VehicleVisionOCR.Domain.VIN.Models;

namespace VehicleVisionOCR.Application.VINReasoning
{
    public class WmiKnowledgeProvider : IWmiKnowledgeProvider
    {
        private readonly Dictionary<string, ManufacturerData> _wmiDatabase;

        public WmiKnowledgeProvider()
        {
            // In a real application, this would load from a local SQLite DB or JSON file at startup.
            // For now, we mock some common ones to ensure the architecture works.
            _wmiDatabase = new Dictionary<string, ManufacturerData>
            {
                { "1HG", new ManufacturerData { WmiCode = "1HG", ManufacturerName = "Honda", Country = "USA", Region = "North America" } },
                { "JHM", new ManufacturerData { WmiCode = "JHM", ManufacturerName = "Honda", Country = "Japan", Region = "Asia" } },
                { "WBA", new ManufacturerData { WmiCode = "WBA", ManufacturerName = "BMW", Country = "Germany", Region = "Europe" } },
                { "ME4", new ManufacturerData { WmiCode = "ME4", ManufacturerName = "Mitsubishi", Country = "India", Region = "Asia" } } // Example Indian chassis
            };
        }

        public bool IsValidWmi(string wmi)
        {
            if (string.IsNullOrEmpty(wmi) || wmi.Length != 3) return false;
            return _wmiDatabase.ContainsKey(wmi);
        }

        public ManufacturerData GetManufacturerData(string wmi)
        {
            if (IsValidWmi(wmi))
            {
                return _wmiDatabase[wmi];
            }
            return null;
        }
    }
}
