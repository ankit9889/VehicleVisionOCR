using System;

namespace VehicleVisionOCR.Infrastructure.Exceptions
{
    public class ConfigurationException : InfrastructureException
    {
        public ConfigurationException(string message) : base(message) { }
        public ConfigurationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
