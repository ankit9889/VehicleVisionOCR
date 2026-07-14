using System;

namespace VehicleVisionOCR.Scanner.Core.Enums
{
    public enum BeepPattern
    {
        Success, // e.g., 1 High Short Beep
        Error,   // e.g., 3 Low Short Beeps
        Warning  // e.g., 2 Low Short Beeps
    }
}
