using System;

namespace VehicleVisionOCR.Infrastructure.Exceptions
{
    public class DatabaseException : InfrastructureException
    {
        public DatabaseException(string message) : base(message) { }
        public DatabaseException(string message, Exception innerException) : base(message, innerException) { }
    }
}
