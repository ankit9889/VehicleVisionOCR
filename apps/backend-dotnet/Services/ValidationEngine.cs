using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace VehicleVisionOCR.Backend.Services
{
    public interface IValidationEngine
    {
        bool ValidateVin(string vin);
        bool ValidateLicensePlate(string plate);
        bool IsConfidenceAcceptable(double confidence, double threshold = 80.0);
    }

    public class ValidationEngine : IValidationEngine
    {
        private readonly ILogger<ValidationEngine> _logger;

        public ValidationEngine(ILogger<ValidationEngine> logger)
        {
            _logger = logger;
        }

        public bool ValidateVin(string vin)
        {
            if (string.IsNullOrWhiteSpace(vin)) return false;

            vin = vin.ToUpper().Trim();
            
            // Standard 17 character VIN check, excluding I, O, Q
            var regex = new Regex(@"^[A-HJ-NPR-Z0-9]{17}$");
            
            bool isValid = regex.IsMatch(vin);
            
            if (!isValid)
            {
                _logger.LogWarning($"VIN Validation failed for: {vin}");
            }
            
            return isValid;
        }

        public bool ValidateLicensePlate(string plate)
        {
            if (string.IsNullOrWhiteSpace(plate)) return false;

            plate = plate.ToUpper().Trim().Replace(" ", "");

            // Simple validation example (e.g., European format 2 letters, 2 numbers, 3 letters)
            // Or general alphanumeric 5-8 chars
            var regex = new Regex(@"^[A-Z0-9]{5,25}$");
            
            return regex.IsMatch(plate);
        }

        public bool IsConfidenceAcceptable(double confidence, double threshold = 80.0)
        {
            return confidence >= threshold;
        }
    }
}
