using System;

namespace VehicleVisionOCR.Scanner.ZebraWindows.Exceptions
{
    public class ZebraScannerException : Exception
    {
        public int StatusCode { get; }

        public ZebraScannerException(string message) : base(message)
        {
        }

        public ZebraScannerException(string message, int statusCode) : base(message)
        {
            StatusCode = statusCode;
        }

        public ZebraScannerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
