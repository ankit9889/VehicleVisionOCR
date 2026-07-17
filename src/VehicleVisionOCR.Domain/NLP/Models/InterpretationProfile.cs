using System.Collections.Generic;

namespace VehicleVisionOCR.Domain.NLP.Models
{
    public class InterpretationProfile
    {
        public bool EnableDictionaryMatching { get; set; } = false;
        
        /// <summary>
        /// The list of valid dictionary terms for this specific region/field.
        /// </summary>
        public List<string> DictionaryTerms { get; set; } = new List<string>();
        
        public int MaxEditDistance { get; set; } = 3;
        
        public bool EnableUnicodeNormalization { get; set; } = true;
        
        public bool EnableOcrConfusionRepair { get; set; } = true;
        
        /// <summary>
        /// Context-free OCR confusion mapping (e.g. "I" to "l")
        /// </summary>
        public Dictionary<string, string> OcrConfusionMatrix { get; set; } = new Dictionary<string, string>();
    }
}
