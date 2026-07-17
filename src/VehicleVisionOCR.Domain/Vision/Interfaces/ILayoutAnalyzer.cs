using System.Collections.Generic;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Domain.Vision.Interfaces
{
    public interface ILayoutAnalyzer
    {
        /// <summary>
        /// Analyzes the structure of an image and returns a list of semantic zones (VIN, Color, Model, etc.) cropped from the original.
        /// </summary>
        IEnumerable<CroppedZone> AnalyzeLayout(byte[] rawImageData);
    }
}
