using System.Collections.Generic;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Domain.Vision.Interfaces
{
    public interface IRegionClassifier
    {
        /// <summary>
        /// Evaluates unclassified regions based on their features and assigns semantic ZoneTypes (VIN, Color, Model, etc.) using Probability Rules.
        /// </summary>
        void ClassifyRegions(List<RegionCandidate> candidates);
    }
}
