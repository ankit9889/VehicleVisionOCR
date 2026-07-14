using System;

namespace VehicleVisionOCR.Infrastructure.Exceptions
{
    public class InfrastructureException : Exception
    {
        public InfrastructureException(string message) : base(message) { }
        public InfrastructureException(string message, Exception innerException) : base(message, innerException) { }
    }
}
