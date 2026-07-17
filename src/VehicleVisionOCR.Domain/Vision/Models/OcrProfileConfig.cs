using System.Collections.Generic;

namespace VehicleVisionOCR.Domain.Vision.Models
{
    public class OcrProfileConfig
    {
        public string Language { get; set; } = "eng";
        
        public string WhitelistedCharacters { get; set; }
        
        /// <summary>
        /// Page Segmentation Modes to run in parallel.
        /// </summary>
        public List<int> PageSegmentationModes { get; set; } = new List<int> { 7 };
        
        public List<int> OcrEngineModes { get; set; } = new List<int> { 3 }; // 3 = Default
        
        public List<double> Scales { get; set; } = new List<double> { 1.0 };
        
        public List<string> PreprocessingPipelines { get; set; } = new List<string> { "Original" };
    }
}
